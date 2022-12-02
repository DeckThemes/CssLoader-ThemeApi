namespace DeckPersonalisationApi.Utils;

public class Utils
{
    public static long DirSize(string path) => DirSize(new DirectoryInfo(path));
    public static long DirSize(DirectoryInfo d) // https://stackoverflow.com/questions/468119/whats-the-best-way-to-calculate-the-size-of-a-directory-in-net
    {    
        long size = 0;    
        // Add file sizes.
        FileInfo[] fis = d.GetFiles();
        foreach (FileInfo fi in fis) 
        {      
            size += fi.Length;    
        }
        // Add subdirectory sizes.
        DirectoryInfo[] dis = d.GetDirectories();
        foreach (DirectoryInfo di in dis) 
        {
            if (di.Name != ".git")
                size += DirSize(di);   
        }
        return size;  
    }
}