using System;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace AshNCircuit.EditorTools
{
    public static class UpmUpdateAiPackages
    {
        private static readonly string[] PackageNames =
        {
            "com.unity.ai.toolkit",
            "com.unity.ai.assistant",
            "com.unity.ai.generators",
            "com.unity.ai.inference",
        };

        public static void UpdateAiPackages()
        {
            Debug.Log("[UpmUpdateAiPackages] Starting AI package update (UPM add latest)...");

            var failures = new List<string>();

            foreach (var packageName in PackageNames)
            {
                AddPackageLatest(packageName, failures);
            }

            if (failures.Count > 0)
            {
                var message = "[UpmUpdateAiPackages] Update finished with failures:\n- " + string.Join("\n- ", failures);
                Debug.LogError(message);
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log("[UpmUpdateAiPackages] Update finished successfully.");
            EditorApplication.Exit(0);
        }

        private static void AddPackageLatest(string packageName, List<string> failures)
        {
            if (string.IsNullOrWhiteSpace(packageName))
            {
                failures.Add("Package name is empty.");
                return;
            }

            Debug.Log($"[UpmUpdateAiPackages] Client.Add({packageName}) ...");
            AddRequest request;
            try
            {
                request = Client.Add(packageName);
            }
            catch (Exception ex)
            {
                failures.Add($"{packageName}: Client.Add threw {ex.GetType().Name}: {ex.Message}");
                return;
            }

            var start = DateTime.UtcNow;
            while (!request.IsCompleted)
            {
                if ((DateTime.UtcNow - start).TotalMinutes > 10)
                {
                    failures.Add($"{packageName}: timeout waiting for UPM AddRequest completion.");
                    return;
                }

                Thread.Sleep(200);
            }

            if (request.Status == StatusCode.Success)
            {
                Debug.Log($"[UpmUpdateAiPackages] OK: {packageName} => {request.Result.packageId}");
                return;
            }

            var error = request.Error;
            failures.Add(
                error != null
                    ? $"{packageName}: {error.errorCode} {error.message}"
                    : $"{packageName}: failed (no error details)."
            );
        }
    }
}
