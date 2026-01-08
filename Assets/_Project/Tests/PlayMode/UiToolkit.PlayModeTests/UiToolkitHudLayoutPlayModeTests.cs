using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace AshNCircuit.Tests.UI
{
    /// <summary>
    /// UI Toolkit の基本レイアウトが崩れていないことを、シーン上で機械的に検査する PlayMode テスト。
    /// </summary>
    public class UiToolkitHudLayoutPlayModeTests : RuntimeUITestFixture
    {
        private const string BootSceneName = "Boot";
        private const string SceneName = "Game";

        [UnityOneTimeSetUp]
        public IEnumerator OneTimeSetup()
        {
            // Game シーンは BootLoader による JSON 初期化（MessageCatalog/Items/Actors など）を前提にしているため、
            // テストでも Boot シーン経由で初期化してから Game へ遷移する。
            yield return SceneManager.LoadSceneAsync(BootSceneName, LoadSceneMode.Single);

            const int maxWaitFrames = 300;
            var waitFrames = 0;
            while (!string.Equals(SceneManager.GetActiveScene().name, SceneName))
            {
                waitFrames++;
                if (waitFrames > maxWaitFrames)
                {
                    Assert.Fail($"Timed out waiting for scene '{SceneName}' (from '{BootSceneName}').");
                }

                yield return null;
            }

            // UI の生成・レイアウト反映を 1 フレーム待つ。
            yield return null;
        }

        [UnityTest]
        public IEnumerator HudView_ElementsExist_AndStayInsidePanel()
        {
            // HudView.uxml が割り当てられている UIDocument を特定する（要素名で判定）。
            var hudDocument = FindDocumentContainingElement("hud-hp-bar-fill");
            SetUIContent(hudDocument);

            // UI Toolkit の更新・レイアウト反映を進める。
            simulate.FrameUpdate();
            yield return null;
            simulate.FrameUpdate();

            AssertElementInsideRoot("hud-hp-bar-fill");
            // XP は現状「未実装の仮表示」で 0% 幅が正しいため、幅 0 を許容する。
            AssertElementInsideRoot("hud-xp-bar-fill", requirePositiveWidth: false);
            AssertElementInsideRoot("hud-minimap-content");

            yield return CaptureScreenshotIfPossible($"{SceneName}_HudView");
        }

        private static UIDocument FindDocumentContainingElement(string elementName)
        {
            var documents = Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
            foreach (var document in documents)
            {
                var root = document.rootVisualElement;
                if (root == null)
                {
                    continue;
                }

                if (root.Q<VisualElement>(elementName) != null)
                {
                    return document;
                }
            }

            Assert.Fail($"UIDocument with VisualElement '{elementName}' was not found in scene '{SceneName}'.");
            return null!;
        }

        private void AssertElementInsideRoot(
            string elementName,
            bool requirePositiveWidth = true,
            bool requirePositiveHeight = true)
        {
            var element = rootVisualElement.Q<VisualElement>(elementName);
            Assert.That(element, Is.Not.Null, $"VisualElement '{elementName}' was not found.");

            Assert.That(
                element.resolvedStyle.display,
                Is.Not.EqualTo(DisplayStyle.None),
                $"VisualElement '{elementName}' is not displayed."
            );

            var rootRect = rootVisualElement.worldBound;
            var elementRect = element.worldBound;

            Assert.That(rootRect.width, Is.GreaterThan(0f), "rootVisualElement.worldBound has invalid width.");
            Assert.That(rootRect.height, Is.GreaterThan(0f), "rootVisualElement.worldBound has invalid height.");
            if (requirePositiveWidth)
            {
                Assert.That(elementRect.width, Is.GreaterThan(0f), $"'{elementName}' has invalid width.");
            }

            if (requirePositiveHeight)
            {
                Assert.That(elementRect.height, Is.GreaterThan(0f), $"'{elementName}' has invalid height.");
            }

            Assert.That(
                rootRect.Contains(elementRect.min) && rootRect.Contains(elementRect.max),
                Is.True,
                $"'{elementName}' is out of root bounds. root={rootRect} element={elementRect}"
            );
        }

        private static IEnumerator CaptureScreenshotIfPossible(string fileBaseName)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                Debug.LogWarning($"[UITest] Screenshot skipped (no graphics device): {fileBaseName}");
                yield break;
            }

            yield return new WaitForEndOfFrame();

            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            if (texture == null)
            {
                Debug.LogWarning($"[UITest] Screenshot skipped (CaptureScreenshotAsTexture returned null): {fileBaseName}");
                yield break;
            }

            try
            {
                var bytes = texture.EncodeToPNG();
                var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                var directory = Path.Combine(projectRoot, "Logs", "PlayModeTests", "Screenshots");
                Directory.CreateDirectory(directory);

                var path = Path.Combine(directory, $"{fileBaseName}.png");
                File.WriteAllBytes(path, bytes);
                Debug.Log($"[UITest] Screenshot saved: {path}");
            }
            finally
            {
                Object.Destroy(texture);
            }
        }
    }
}
