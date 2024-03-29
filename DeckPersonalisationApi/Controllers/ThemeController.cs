﻿using System.IO.Compression;
using DeckPersonalisationApi.Exceptions;
using DeckPersonalisationApi.Extensions;
using DeckPersonalisationApi.Middleware.JwtRole;
using DeckPersonalisationApi.Model;
using DeckPersonalisationApi.Model.Dto.External.GET;
using DeckPersonalisationApi.Model.Dto.External.POST;
using DeckPersonalisationApi.Model.Dto.Internal.GET;
using DeckPersonalisationApi.Services;
using DeckPersonalisationApi.Services.Css;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DeckPersonalisationApi.Controllers;

[ApiController]
[Route("themes")]
public class ThemeController : Controller
{
    private JwtService _jwt;
    private UserService _user;
    private ThemeService _theme;

    public ThemeController(JwtService jwt, ThemeService theme, UserService user)
    {
        _jwt = jwt;
        _theme = theme;
        _user = user;
    }

    [HttpGet]
    public IActionResult GetThemes(int page = 1, int perPage = 50, string filters = "", string order = "", string search = "")
    {
        PaginationDto paginationDto = new(page, perPage, filters, order, search);
        PaginatedResponse<CssTheme> response = _theme.GetApprovedThemes(paginationDto);
        return response.Ok();
    }
    
    [HttpGet("filters")]
    public IActionResult GetThemesFilters(string type = "")
        => new PaginationFilters(_theme.FiltersWithCount(type, null), _theme.Orders().ToList()).Ok();
    
    [HttpGet("awaiting_approval/filters")]
    [Authorize]
    [JwtRoleRequire(Permissions.ViewThemeSubmissions)]
    public IActionResult GetUnapprovedThemesFilters(string type = "")
        => new PaginationFilters(_theme.FiltersWithCount(type, null, visibility: PostVisibility.Private), _theme.Orders().ToList()).Ok();

    [HttpGet("awaiting_approval")]
    [Authorize]
    [JwtRoleRequire(Permissions.ViewThemeSubmissions)]
    public IActionResult GetAwaitingApprovalThemes(int page = 1, int perPage = 50, string filters = "", string order = "", string search = "")
    {
        PaginationDto paginationDto = new(page, perPage, filters, order, search);
        PaginatedResponse<CssTheme> response = _theme.GetNonApprovedThemes(paginationDto);
        return response.Ok();
    }

    [HttpGet("ids")]
    public IActionResult GetThemes(string ids) // ids is split on `.`
    {
        List<string> idsList = ids.Split('.').Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

        if (idsList.Count <= 0)
            return new List<string>().Ok();

        return _theme.GetThemesByIds(idsList, false).Select(x => ((IToDto<MinimalCssThemeDto>)x).ToDto()).Ok();
    }

    [HttpGet("{id}")]
    public IActionResult GetTheme(string id)
        => ((IToDto<FullCssThemeDto>)_theme.GetThemeById(id, false).Require("Theme not found")).ToDto().Ok();

    [HttpPatch("{id}")]
    [Authorize]
    [JwtRoleRequire(Permissions.EditAnyPost)]
    [JwtRoleReject(Permissions.FromApiToken)]
    public IActionResult EditTheme(string id, CssThemeDirectPatchDto patch)
    {
        CssTheme theme = _theme.GetThemeById(id).Require();
        User? author = (patch.Author == null) ? null : _user.GetActiveUserById(patch.Author).Require();
        _theme.EditTheme(theme, patch.Description, patch.Target, author);
        return new OkResult();
    }

    [HttpDelete("{id}")]
    [Authorize]
    [JwtRoleReject(Permissions.FromApiToken)]
    public IActionResult DeleteTheme(string id)
    {
        UserJwtDto jwt = _jwt.DecodeToken(Request).Require();
        CssTheme theme = _theme.GetThemeById(id).Require();

        if (!jwt.HasPermission(Permissions.EditAnyPost) && theme.Author.Id != jwt.Id)
            throw new NotFoundException("Could not find theme");
        
        _theme.DeleteTheme(theme, true, true);
        return new OkResult();
    }

    [HttpGet("legacy/audio")]
    public IActionResult GetAudioPacksAsLegacy(bool approved = true)
        =>  _theme.GetThemesLegacy(ThemeType.Audio, approved ? PostVisibility.Public : PostVisibility.Private).Ok();

    [HttpGet("legacy/css")]
    public IActionResult GetCssThemesAsLegacy(bool approved = true)
        =>  _theme.GetThemesLegacy(ThemeType.Css, approved ? PostVisibility.Public : PostVisibility.Private).Ok();

    [HttpGet("template/css")]
    public IActionResult GetCssTemplateTheme(string themeName = "New Theme")
    {
        if (new List<char>(){ '"', '\\', '/', ':', '*', '?', '<', '>', '|'}.Any(themeName.Contains))
            throw new BadRequestException("Theme name cannot include invalid characters");

        string jsonStr = System.IO.File.ReadAllText("csstemplate.json")
            .Replace("%NAME%", themeName);
        
        JObject json = JsonConvert.DeserializeObject<JObject>(jsonStr)!;

        var memoryStream = new MemoryStream();
        
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            var jsonFile = archive.CreateEntry($"{themeName}/theme.json");
            using (var entryStream = jsonFile.Open())
            using (var streamWriter = new StreamWriter(entryStream))
            {
                streamWriter.Write(json);
            }
            
            foreach (var x in json["inject"] as JObject)
            {
                archive.CreateEntry($"{themeName}/{x.Key}");
            }
        }

        memoryStream.Seek(0, SeekOrigin.Begin);
        return new FileStreamResult(memoryStream, "application/zip");
    }
}