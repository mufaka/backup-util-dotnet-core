# KronoMata.Plugins.Backup
This is an IPlugin implementation for [KronoMata](https://github.com/mufaka/KronoMata) that adapts the [backup-util-dotnet-core](https://github.com/freedom35/backup-util-dotnet-core) Console application for use in KronoMata.

## Warning
If you use the 'sync' backup type, ensure that the target directory (target_dir) is empty or only includes files expected to be in the source directories (source_dirs). The sync backup type will delete files from the target directory that do not exist in the source directories.

## Sample Configuration

![image](https://github.com/mufaka/backup-util-dotnet-core/assets/8632538/095a09ad-5742-4f77-953c-89ef69fee9fd)

```
﻿---

# Backup type (Copy/Sync/Isolated)
backup_type: sync

# Max number of days to keep backups when using 'isolated' backup type 
# (zero for no limit)
max_isolation_days: 14

# Hidden files
ignore_hidden_files: false

# Specify root target directory for backup
target_dir: \\10.10.11.1\USB_Storage\Backup

# Add each source directory to backup...
source_dirs:
 - D:\KronoMata\Publish\Web\Database
 - D:\KronoMata\Publish\Web\PackageRoot
 - D:\JupyterRoot

# Add any excluded directories
excluded_dirs: []

# Add extensions for excluded file types
excluded_types:
 - zip 

...
```

### A note on SMB shares
That target_dir must be accessible from the command line on the Agent host system. Therefore, UNC paths will only work on Windows systems while mount points should be used on Linux and MacOS. The configuration for target_dir above is for Windows. On MacOS, the same share would be defined as follows:

```
target_dir: /Volumes/USB_Storage/Backup
```

*Be sure to take heed of the warning at the top of the page. You should define unique root paths on the backup device for the target_dir of each Agent that will perform a backup.*

## Configuration Settings
The following settings can be configured within the YAML configuration file. Most settings are required to be defined within the configuration file. If any critical setting is missing, or the value is inappropriate, then the settings will not validate and the backup will not be run and the app will exit with an error.

<br /> 

### ***backup_type***
Determines the type of backup to execute *(see table below)*.  

Setting is required. 


|Types|Description|
|:---:|-----|
|**copy**|Copies the contents of the source directory to the target directory. Any files later deleted from the source directory, will remain in the target directory.|
|**sync**|Keeps the target directory in-sync with the source directory. Files deleted from the source directory will also be deleted from the target directory.|
|**isolated**|Creates isolated backups within the target directory. I.e. Each time a backup is run, a new/separate backup copy is created.|

*Example config entries:*
```yaml
backup_type: copy
```
```yaml
backup_type: sync
```

<br />

### ***target_dir***
Defines the path of the root target backup directory, where the backup will take place.  

Setting is required: Must have a target directory in order to back-up.

*Example config entries:*
```yaml
target_dir: C:\Backups
```
```yaml
target_dir: /Users/freedom35/Backups
```

<br />

### ***source_dirs***
Determines the list of source directories that will be backed up.  

Setting is required: Must have at least one source directory to back-up.

*Example config entries:*
```yaml
source_dirs:
 - C:\Users\freedom35\Projects
 - C:\Users\freedom35\Documents\Specs
```
```yaml
source_dirs:
 - /Users/freedom35/Projects
 - /Users/freedom35/Documents/Specs
```

<br />  

### ***max_isolation_days***
Integer value determining the max number of days to keep existing backups.  
This setting is only used when ***isolated*** is configured as the backup type.  

Set to zero for no max limit (default value).  

*Example config entries:*
```yaml
max_isolation_days: 0
```
```yaml
max_isolation_days: 30
```

<br />

### ***ignore_hidden_files***
Determines whether hidden files and folders are ignored during a backup run.  

Default Value: *true*

*Example config entries:*
```yaml
ignore_hidden_files: true
```
```yaml
ignore_hidden_files: false
```

<br />

### ***excluded_dirs***
Determines the list of directories (or sub-directories) that will be ***excluded*** from the backup.  

These directories will not be copied or synced. This can be useful when saving on target storage space.  

Default Value: *None*

*Example config entries:*
```yaml
excluded_dirs:
 - obj
 - bin
 - _sgbak
 - .vs
```
```yaml
excluded_dirs: []
```

<br />

### ***excluded_types***
Determines the list of file types/extensions that will be ***excluded*** from the backup.  

Files with these extensions will not be copied or synced. This can be useful when saving on target storage space.  

Default Value: *None*

*Example config entries:*
```yaml
excluded_types:
 - dll
 - pdb
 - zip
```
```yaml
excluded_types: []
```
