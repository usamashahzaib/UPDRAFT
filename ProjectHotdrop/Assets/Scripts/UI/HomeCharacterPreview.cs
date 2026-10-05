using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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
        readonly GameObject avatar;
        // Qualify this because Hotdrop.Camera is also a project namespace.
        readonly UnityEngine.Camera camera;
        static int stageNumber;

        public HomeCharacterPreview(int width = 448, int height = 560, Player.BodyRig? rig = null)
        {
            Texture = new RenderTexture(width, height, 16) { name = "HomeCharacterPreview" };
            stage = new GameObject("HomePreviewStage");
            stage.transform.position = new Vector3((stageNumber++ % 100) * 100f, -500f, 0f);

            avatar = new GameObject("PreviewAvatar");
            avatar.transform.SetParent(stage.transform, false);
            var parts = Player.AvatarRigVisual.Build(avatar, rig ?? Player.BodyRigStore.Current,
                Resources.Load<Material>("Materials/Friendly"),
                Resources.Load<Material>("Materials/Equipment"), includeGun: true);
            var motion = avatar.AddComponent<Player.SportAvatarAnimation>();
            Player.AvatarRigVisual.Wire(motion, parts);
            // Amendment 1/3: the Home card must show the same equipped identity as in play —
            // body style, weapon wrap and badge together, not one cached slot.
            CosmeticLook.Apply(avatar.transform, CosmeticLook.LocalComposite);

            var cameraObj = new GameObject("HomePreviewCamera");
            cameraObj.transform.SetParent(stage.transform, false);
            camera = cameraObj.AddComponent<UnityEngine.Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            // The runner sits over the Home venue, so the render texture must keep
            // transparent pixels instead of drawing a dark portrait rectangle.
            camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            camera.fieldOfView = 32f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 30f;
            camera.transform.localPosition = new Vector3(0.78f, 1.25f, 3.25f);
            camera.transform.LookAt(avatar.transform.position + Vector3.up * .93f);
            camera.targetTexture = Texture;
            var light = new GameObject("PreviewLight", typeof(Light)).GetComponent<Light>();
            light.transform.SetParent(stage.transform, false);
            light.type = LightType.Point; light.range = 8f; light.intensity = 3f;
            light.transform.localPosition = new Vector3(-1.5f, 3f, 2f);
        }

        /// <summary>FULL BODY or FACE framing, as the appearance screens' framing toggle.</summary>
        public void SetFraming(bool face)
        {
            if (camera == null || avatar == null) return;
            camera.fieldOfView = face ? 11f : 32f;
            camera.transform.localPosition = face ? new Vector3(0.5f, 1.6f, 3.25f) : new Vector3(0.78f, 1.25f, 3.25f);
            camera.transform.LookAt(avatar.transform.position + Vector3.up * (face ? 1.58f : .93f));
        }

        public void Rotate(float degrees)
        {
            if (avatar != null) avatar.transform.Rotate(Vector3.up, degrees, Space.Self);
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
            var image = obj.GetComponent<RawImage>();
            image.raycastTarget = false;
            return image;
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

    public sealed class HomePreviewDrag : MonoBehaviour, IDragHandler
    {
        public HomeCharacterPreview Preview;

        public void OnDrag(PointerEventData eventData)
        {
            Preview?.Rotate(-eventData.delta.x * 0.7f);
        }
    }
}


