using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class WebGlPublish
{
    const string Scene = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Final Leap/Publish Web")]
    public static void PublishFromMenu()
    {
        string error = Build(false);
        if (string.IsNullOrEmpty(error))
            Debug.Log("웹 빌드를 docs 폴더에 넣었습니다.");
        else
            Debug.LogError(error);
    }

    public static void Build()
    {
        string error = Build(true);
        EditorApplication.Exit(string.IsNullOrEmpty(error) ? 0 : 1);
    }

    static string Build(bool batch)
    {
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.threadsSupport = false;
        PlayerSettings.WebGL.initialMemorySize = 256;
        PlayerSettings.WebGL.template = "APPLICATION:Default";

        string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "docs"));
        if (Directory.Exists(outDir))
            Directory.Delete(outDir, true);
        Directory.CreateDirectory(outDir);

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { Scene },
            locationPathName = outDir,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        });

        if (report.summary.result != BuildResult.Succeeded)
            return "WebGL 빌드 실패: " + report.summary.result;

        string indexPath = Path.Combine(outDir, "index.html");
        if (File.Exists(indexPath))
        {
            string html = File.ReadAllText(indexPath);
            html = html.Replace("<title>Unity WebGL Player | Final_Leap</title>", "<title>마지막 도약</title>");
            File.WriteAllText(indexPath, html);
        }

        File.WriteAllText(Path.Combine(outDir, ".nojekyll"), "");
        if (batch)
            Debug.Log("WebGL build written to " + outDir);
        return null;
    }
}
