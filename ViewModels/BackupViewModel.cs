using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AINovel.Models;
using AINovel.Services;
using Microsoft.Win32;

namespace AINovel.ViewModels;

public partial class BackupViewModel : ViewModelBase
{
    private readonly SystemConfig _config;

    [ObservableProperty]
    private string _backupPath = string.Empty;

    [ObservableProperty]
    private int _backupRetentionDays = 7;

    [ObservableProperty]
    private ObservableCollection<string> _backupHistory = new();

    [ObservableProperty]
    private string _selectedBackupFile = string.Empty;

    public BackupViewModel(SystemConfig config)
    {
        _config = config;
        BackupPath = config.BackupPath;
        BackupRetentionDays = config.BackupRetentionDays;
        LoadBackupHistory();
    }

    private void LoadBackupHistory()
    {
        BackupHistory.Clear();

        if (!Directory.Exists(BackupPath))
        {
            Directory.CreateDirectory(BackupPath);
        }

        var files = Directory.GetFiles(BackupPath, "*.db", SearchOption.TopDirectoryOnly)
            .OrderByDescending(f => File.GetCreationTime(f))
            .ToList();

        foreach (var file in files)
        {
            BackupHistory.Add(file);
        }
    }

    [RelayCommand]
    private void SelectBackupPath()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "选择备份文件夹"
        };

        if (dialog.ShowDialog() == true)
        {
            BackupPath = dialog.FolderName;
        }
    }

    [RelayCommand]
    private void ManualBackup()
    {
        try
        {
            var dbPath = DbHelper.Db.CurrentConnectionConfig.ConnectionString;
            dbPath = dbPath.Replace("Data Source=", "");

            if (!Directory.Exists(BackupPath))
            {
                Directory.CreateDirectory(BackupPath);
            }

            var backupFileName = $"backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
            var backupFullPath = Path.Combine(BackupPath, backupFileName);

            File.Copy(dbPath, backupFullPath, true);

            StatusMessage = $"备份成功: {backupFullPath}";
            LoadBackupHistory();
        }
        catch (Exception ex)
        {
            StatusMessage = $"备份失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private void RestoreBackup()
    {
        if (string.IsNullOrEmpty(SelectedBackupFile))
        {
            StatusMessage = "请选择要恢复的备份文件";
            return;
        }

        var result = System.Windows.MessageBox.Show(
            "恢复备份将覆盖当前数据，确定要继续吗？",
            "确认恢复",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result != System.Windows.MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var dbPath = DbHelper.Db.CurrentConnectionConfig.ConnectionString;
            dbPath = dbPath.Replace("Data Source=", "");

            File.Copy(SelectedBackupFile, dbPath, true);

            StatusMessage = "恢复成功，请重启程序";
        }
        catch (Exception ex)
        {
            StatusMessage = $"恢复失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private void DeleteOldBackups()
    {
        try
        {
            var cutoffDate = DateTime.Now.AddDays(-BackupRetentionDays);
            var oldFiles = Directory.GetFiles(BackupPath, "*.db")
                .Where(f => File.GetCreationTime(f) < cutoffDate)
                .ToList();

            foreach (var file in oldFiles)
            {
                File.Delete(file);
            }

            StatusMessage = $"已删除 {oldFiles.Count} 个过期备份";
            LoadBackupHistory();
        }
        catch (Exception ex)
        {
            StatusMessage = $"清理失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private void SaveSettings()
    {
        var config = DbHelper.Db.Queryable<SystemConfig>().First();
        if (config != null)
        {
            config.BackupPath = BackupPath;
            config.BackupRetentionDays = BackupRetentionDays;
            config.UpdateTime = DateTime.Now;
            DbHelper.Db.Updateable(config).ExecuteCommand();
            StatusMessage = "备份设置已保存";
        }
    }
}