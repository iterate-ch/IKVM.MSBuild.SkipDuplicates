using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

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
            FileStream fs = null;
            try
            {
                fs = File.OpenRead(referenceItem.ItemSpec);
                using var reader = new PEReader(fs);
                var metadata = reader.GetMetadataReader();
                foreach (var resourceHandle in metadata.ManifestResources)
                {
                    var resource = metadata.GetManifestResource(resourceHandle);
                    var name = metadata.GetString(resource.Name);
                    if (name.Substring(Math.Max(0, name.LastIndexOf('.'))).Equals(".jar"))
                    {
                        resourceJars.Add(name);
                    }
                }
            }
            catch { continue; }
            finally
            {
                fs?.Dispose();
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
