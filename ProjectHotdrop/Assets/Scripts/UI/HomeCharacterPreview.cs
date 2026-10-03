using UnityEngine;
using UnityEngine.UI;

namespace Hotdrop.UI
{
    /// <summary>
    /// Amendment 1: Home shows the actual in-game model and the currently selected runner, not a
    /// promotional stand-in. The avatar is built by the same AvatarRigVisual the match uses, on a
    /// staging platform far below the arena, and rendered into a texture for the Home card.
    /// </summary>
    public class HomeCharacterPreview
    {
        public RenderTexture Texture { get; }
        readonly GameObject stage;
        // Qualify this because Hotdrop.Camera is also a project namespace.
        readonly UnityEngine.Camera camera;

        public HomeCharacterPreview(int width = 448, int height = 560)
        {
            Texture = new RenderTexture(width, height, 16) { name = "HomeCharacterPreview" };
            stage = new GameObject("HomePreviewStage");
            stage.transform.position = new Vector3(0f, -500f, 0f); // below the arena, nothing else in frame

            var avatar = new GameObject("PreviewAvatar");
            avatar.transform.SetParent(stage.transform, false);
            var parts = Player.AvatarRigVisual.Build(avatar, Player.BodyRigStore.Current,
                Resources.Load<Material>("Materials/Friendly"),
                Resources.Load<Material>("Materials/Equipment"), includeGun: true);
            var motion = avatar.AddComponent<Player.SportAvatarAnimation>();
            Player.AvatarRigVisual.Wire(motion, parts);

            var cameraObj = new GameObject("HomePreviewCamera");
            cameraObj.transform.SetParent(stage.transform, false);
            camera = cameraObj.AddComponent<UnityEngine.Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = MenuLayout.Background;
            camera.fieldOfView = 32f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 30f;
            camera.transform.localPosition = new Vector3(0.78f, 1.30f, -2.6f);
            camera.transform.LookAt(avatar.transform.position + Vector3.up * 1.08f);
            camera.targetTexture = Texture;
        }

        /// <summary>A RawImage sized to the preview texture's aspect.</summary>
        public static RawImage BuildCard(Transform parent)
        {
            var obj = new GameObject("RunnerPreview", typeof(RectTransform), typeof(RawImage), typeof(LayoutElement), typeof(AspectRatioFitter));
            obj.transform.SetParent(parent, false);
            var element = obj.GetComponent<LayoutElement>();
            // Keep a portrait that leaves an actual row for the runner label and
            // change action on short landscape phones. Fit the texture inside the
            // layout group's allocated height so the fitter cannot overlap siblings.
            element.minHeight = 190f; element.preferredHeight = 248f; element.flexibleWidth = 1f;
            var fitter = obj.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
            fitter.aspectRatio = 448f / 560f;
            return obj.GetComponent<RawImage>();
        }

        public void Dispose()
        {
            if (camera != null) Object.Destroy(camera.gameObject);
            if (stage != null) Object.Destroy(stage.gameObject);
            if (Texture != null)
            {
                Texture.Release();
                Object.Destroy(Texture);
            }
        }
    }
}


