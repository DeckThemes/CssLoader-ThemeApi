# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY *.sln .
COPY DeckPersonalisationApi/*.csproj ./DeckPersonalisationApi/
RUN dotnet restore

# copy everything else and build app
COPY DeckPersonalisationApi/. ./DeckPersonalisationApi/
WORKDIR /source/DeckPersonalisationApi
RUN dotnet publish -c release -o /app

# Set up VNU
WORKDIR /
ADD https://github.com/validator/validator/releases/download/20.6.30/vnu.linux.zip /
RUN apt update
RUN apt install -y unzip
RUN unzip /vnu.linux.zip
RUN mv /vnu-runtime-image /vnu

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY --from=build /app ./
COPY --from=build /vnu /vnu
RUN chmod a+x /vnu/bin/vnu

RUN apt update
RUN apt install -y git
RUN apt clean

ENV Config__VnuPath=/vnu/bin/vnu
ENV Config__Port=80
EXPOSE 80
ENTRYPOINT ["dotnet", "DeckPersonalisationApi.dll"]