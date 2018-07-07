using System.Collections.Generic;
using System.IO;
using Renci.SshNet;

class SFTPData
{
    public string Host { get; private set; }
    public string User { get; private set; }
    public string Password { get; private set; }

    public SFTPData(string host, string user, string password)
    {
        Host = host;
        User = user;
        Password = password;
    }
}

class SFTPAgent
{
    static SftpClient sftp;

    public SFTPAgent(SFTPData data)
    {
        sftp = new SftpClient(data.Host, data.User, data.Password);
        sftp.Connect();
    }

    public void UploadFile(string localPath, string remotePath)
    {
        sftp.UploadFile(File.OpenRead(localPath), remotePath);
    }

    public void DownloadFile(string localPath, string remotePath)
    {
        sftp.DownloadFile(remotePath, File.OpenWrite(localPath));
    }

    public void Delete(string remotePath)
    {
        sftp.Delete(remotePath);
    }

    public void MoveFile(string remotePath, string newPath)
    {
        sftp.RenameFile(remotePath, newPath);
    }

    public void CreateDirectory(string remotePath)
    {
        sftp.CreateDirectory(remotePath);
    }

    public bool Exists(string remotePath)
    {
        return sftp.Exists(remotePath);
    }

    public void UploadDirectory(string localPath, string remotePath)
    {
        string[] filePaths = Directory.GetFiles(localPath);
        string[] directoryPaths = Directory.GetDirectories(localPath);
        localPath = handleVariousPathEnding(localPath);
        remotePath = handleVariousPathEnding(remotePath);
        for (int i = 0; i < filePaths.Length; i++)
        {
            string fileName = filePaths[i].Remove(0,filePaths[i].LastIndexOf("/") + 1);
            UploadFile(localPath + fileName, remotePath + fileName);
        }
        for (int i = 0; i< directoryPaths.Length; i++)
        {
            string directoryName = directoryPaths[i].Remove(0,directoryPaths[i].LastIndexOf("/") + 1);
            CreateDirectory(remotePath + directoryName);
            UploadDirectory(localPath + directoryName, remotePath + directoryName);
        }
    }

    string handleVariousPathEnding(string path)
    {
        if (path[path.Length-1] != '/')
        {
            path += "/";
        }
        return path;
    }

    public void DownloadDirectory(string localPath, string remotePath)
    {
        localPath = handleVariousPathEnding(localPath); //support for / and non-/ ending paths
        List<Renci.SshNet.Sftp.SftpFile> files = new List<Renci.SshNet.Sftp.SftpFile>();
        files = (List<Renci.SshNet.Sftp.SftpFile>)sftp.ListDirectory(remotePath);
        for (int i = 0; i < files.Count; i++)
        {
            if(checkIfFakeDirectory(files[i].Name))
            {
                continue;
            }
            string destinationPath = localPath + files[i].Name;
            if (files[i].IsDirectory)
            {
                Directory.CreateDirectory(destinationPath);
                DownloadDirectory(destinationPath, files[i].FullName);
            }
            else
            {
                DownloadFile(destinationPath, files[i].FullName);
            }
        }
    }

    bool checkIfFakeDirectory(string directoryName)
    {
        string[] exceptions = { "..", "." };
        for (int j = 0; j < exceptions.Length; j++)
        {
            if (directoryName == exceptions[j])
            {
                return true;
            }
        }
        return false;
    }

    ~SFTPAgent()
    {
        sftp.Disconnect();
        sftp.Dispose();
    }
}
