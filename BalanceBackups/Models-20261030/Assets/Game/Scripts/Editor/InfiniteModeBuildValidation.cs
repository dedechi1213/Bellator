using System;
using System.IO;
using Bellator.Match;
using Bellator.Classes;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Bellator.Editor
{
    [InitializeOnLoad]
    public static class InfiniteModeBuildValidation
    {
        static InfiniteModeBuildValidation() => EditorApplication.update+=Check;
        static void Check()
        {
            if(EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Temp/BellatorInfinite.build")) return;
            File.Delete("Temp/BellatorInfinite.build"); Build();
        }
        [MenuItem("Bellator/Build Development Player for Infinite Validation")]
        public static void Build()
        {
            try
            {
                Directory.CreateDirectory("Temp/BellatorInfinitePlayer");
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes=new[]{GameInterface.MainScene,PlayerClassController.SelectionScene,PlayerClassController.ArenaScene},
                    locationPathName="Temp/BellatorInfinitePlayer/Bellator.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development });
                File.WriteAllText("Temp/BellatorInfinite.build-result.txt",report.summary.result+"; errors="+report.summary.totalErrors+"; bytes="+report.summary.totalSize+"; time="+report.summary.totalTime);
                if(report.summary.result!=BuildResult.Succeeded) Debug.LogError("Bellator validation player build failed: "+report.summary.result);
            }
            catch(Exception error) { File.WriteAllText("Temp/BellatorInfinite.build-result.txt","FAIL: "+error);Debug.LogException(error); }
        }
    }
}
