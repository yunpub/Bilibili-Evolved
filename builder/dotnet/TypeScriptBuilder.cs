using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BilibiliEvolved.Build
{
  partial class ProjectBuilder
  {
    public ProjectBuilder BuildTypeScripts()
    {
      var tsc = new TypeScriptCompiler();
      var uglifyJs = new JavascriptMinifier();
      var files = ResourceMinifier.GetFiles(file =>
        file.FullName.Contains(@"src\") &&
        file.Extension == ".ts" &&
        !file.Name.EndsWith(".d.ts")
      );
      using (var cache = new BuildCache())
      {
        var changedFiles = files.Where(file => !cache.Contains(file)).ToArray();
        if (changedFiles.Any())
        {
          changedFiles.ForEach(file =>
          {
            cache.AddCache(file);
            WriteInfo($"TypeScript build: {file}");
          });
          Console.Write(tsc.Run().Trim());
          Parallel.ForEach(changedFiles.Select(f => ".ts-output/" + f.Replace(".ts", ".js").Replace($"src{Path.DirectorySeparatorChar}", "")), file => {
            var text = RegexReplacer.Replace(File.ReadAllText(file), @"import\(\(\(\)\s*=>\s*(.*)\)\(\)\)", match => {
              Console.WriteLine($"import({match.Groups[1].Value})");
              return $"import({match.Groups[1].Value})";
            });
            var min = uglifyJs.Minify(text);
            var minFile = ResourceMinifier.GetMinimizedFileName(file);
            File.WriteAllText(minFile, min);
            WriteHint($"\t=> {minFile}");
          });
        }
        cache.SaveCache();
      }
      WriteSuccess("TypeScript build complete.");
      return this;
    }
  }
}
