using BackupUtilityCore.Tasks;
using BackupUtilityCore;
using KronoMata.Public;
using System.Text;

namespace KronoMata.Plugins.Backup
{
    /// <summary>
    /// This plugin implementation aims to keep most of the original source of 'backup-util-dotnet-core' in tact as a form of credit
    /// to the original author Alan Barr. Original source has been forked in this repository to match a lesser version of .NET used
    /// by KronoMata as well as doing minimal refactoring of YAML loading to allow for reading from a string where the original was
    /// tied to loading from a file.
    /// 
    /// Additionally, it is normally frowned upon to reference a Console application from another assembly but that is required to
    /// keep true to the above stated goal. 
    /// 
    /// To do this the 'correct' way would be to refactor the Console application to include a class library that handles the work of 
    /// the backup and reference that or to just copy paste needed code from backup-util-dotnet-core as needed. Neither of those options seems appropriate 
    /// here considering the work done by Alan (first commit 3 years ago at the time of writing this plugin, solid test coverage, and with continuous updates)
    /// </summary>
    public class BackupPlugin : IPlugin
    {
        private StringBuilder _backupTaskLog = new StringBuilder();

        public string Name { get { return "File Backup"; } }

        public string Description { get { return "Adapts Alan Barr's (freedom35 on GitHub) backup-util-dotnet-core Console application as a KronoMata Plugin."; } }

        public string Version { get { return "1.0"; } }

        public List<PluginParameter> Parameters
        {
            get
            {
                var parameters = new List<PluginParameter>();

                parameters.Add(new PluginParameter()
                {
                    Name = "YAML Configuration",
                    Description = "See https://github.com/mufaka/backup-util-dotnet-core for documentation.",
                    DataType = ConfigurationDataType.Text,
                    IsRequired = true
                });

                return parameters;
            }
        }

        private PluginResult? ValidateRequiredParameters(Dictionary<string, string> pluginConfig)
        {
            PluginResult? missingRequiredParameterResult = null;

            foreach (PluginParameter parameter in Parameters)
            {
                if (parameter.IsRequired && !pluginConfig.ContainsKey(parameter.Name))
                {
                    missingRequiredParameterResult ??= new PluginResult()
                    {
                        IsError = true,
                        Message = "Missing required parameter(s).",
                        Detail = "The plugin configuration is missing the following parameters:"
                    };

                    missingRequiredParameterResult.Detail = missingRequiredParameterResult.Detail + Environment.NewLine + parameter.Name;
                }
            }

            return missingRequiredParameterResult;
        }

        public List<PluginResult> Execute(Dictionary<string, string> systemConfig, Dictionary<string, string> pluginConfig)
        {
            var log = new List<PluginResult>();

            try
            {
                var invalidConfigurationResult = ValidateRequiredParameters(pluginConfig);

                if (invalidConfigurationResult != null)
                {
                    log.Add(invalidConfigurationResult);
                }
                else
                {
                    // do we have valid YAML?
                    var yaml = pluginConfig["YAML Configuration"];

                    if (BackupSettings.TryParseFromYamlString(yaml, out BackupSettings backupSettings))
                    {
                        // Create backup object
                        BackupTaskBase backupTask = CreateBackupTask(backupSettings.BackupType);

                        // Add handler for output
                        backupTask.Log += BackupTask_Log;

                        try
                        {
                            // Execute backup
                            backupTask.Run(backupSettings);
                        }
                        finally
                        {
                            // Remove handler
                            backupTask.Log -= BackupTask_Log;
                        }

                        // Return error if backup had issues
                        if (backupTask.CompletedWithoutError)
                        {
                            log.Add(new PluginResult()
                            {
                                IsError = false,
                                Message = "Backup Succeeded.",
                                Detail = _backupTaskLog.ToString()
                            });
                        }
                        else
                        {
                            log.Add(new PluginResult()
                            {
                                IsError = true,
                                Message = "Backup Failed.",
                                Detail = _backupTaskLog.ToString()
                            });
                        }
                    }
                    else
                    {
                        // Add some additional info to log...
                        var configErrorBuff = new StringBuilder();

                        foreach (KeyValuePair<string, string> invalidSetting in backupSettings.GetInvalidSettings())
                        {
                            configErrorBuff.AppendLine($"{invalidSetting.Key}: {invalidSetting.Value}");
                        }

                        log.Add(new PluginResult()
                        {
                            IsError = true,
                            Message = "YAML configuration is invalid.",
                            Detail = configErrorBuff.ToString()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                log.Add(new PluginResult()
                {
                    IsError = true,
                    Message = ex.Message,
                    Detail = ex.StackTrace ?? String.Empty
                });
            }

            return log;
        }

        private void BackupTask_Log(object _, MessageEventArgs e)
        {
            _backupTaskLog.AppendLine(e.ToString());
        }

        private static BackupTaskBase CreateBackupTask(BackupType backupType)
        {
            return backupType switch
            {
                BackupType.Copy => new BackupTaskCopy(),
                BackupType.Sync => new BackupTaskSync(),
                BackupType.Isolated => new BackupTaskIsolatedCopy(),
                _ => throw new NotImplementedException($"Backup task not implemented for '{backupType}'.")
            };
        }
    }
}
