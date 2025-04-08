using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace IKVM.MSBuild.SkipDuplicates;

public class IkvmSkipDuplicateConvert : Task
{
    [Required]
    public ITaskItem[] Convert { get; set; }

    [Required]
    public ITaskItem[] References { get; set; }

    [Output]
    public ITaskItem[] Filtered { get; set; }

    public override bool Execute()
    {
        HashSet<string> resourceJars = new(StringComparer.OrdinalIgnoreCase);
        foreach (var referenceItem in References)
        {
            Assembly assembly;
            try
            {
                assembly = Assembly.LoadFile(referenceItem.ItemSpec);
            }
            catch
            {
                continue;
            }

            foreach (var item in assembly.GetManifestResourceNames())
            {
                if (item.Substring(Math.Max(0, item.LastIndexOf('.'))).Equals(".jar"))
                {
                    resourceJars.Add(item);
                }
            }
        }

        Filtered = Array.FindAll(Convert, item =>
        {
            var fileNamePart = item.ItemSpec.Substring(item.ItemSpec.LastIndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]) + 1);
            return fileNamePart.Substring(Math.Max(0, fileNamePart.LastIndexOf('.'))).Equals(".jar", StringComparison.OrdinalIgnoreCase)
                && resourceJars.Contains(fileNamePart);
        });

        return true;
    }
}
