using UnityEngine;

namespace TetrisArcade
{
    /// <summary>
    /// The picture backdrop behind the playfield.
    ///
    /// Loaded as a Texture2D and turned into a sprite here rather than loaded as
    /// a Sprite asset, because the import settings are Multiple sprite mode and
    /// would not hand back a single usable sprite. Building it at runtime also
    /// matches how the rest of the game makes its sprites.
    /// </summary>
    public partial class TetrisGame
    {
        // Any image dropped in Assets/Resources/pic_assests is a candidate; the
        // first one found wins, so swapping the backdrop is a file swap.
        const string BackdropFolder = "pic_assests";

        // Held well back so the picture sits behind the game rather than competing
        // with it — the playfield is translucent, and a bright backdrop shows
        // straight through and washes the board out.
        static readonly Color BackdropTint = new Color(0.42f, 0.42f, 0.46f, 1f);

        // Which height of the picture, measured from the top, should sit at the
        // centre of the view. The current art has its eyes about 26% down, so
        // anchoring here keeps the face in frame when the sides are cropped.
        // 0 = top edge, 0.5 = centred, 1 = bottom edge.
        const float BackdropFocusY = 0.30f;

        SpriteRenderer _backdrop;
        float _backdropAspect = 1f;

        void SetupBackdrop()
        {
            var textures = Resources.LoadAll<Texture2D>(BackdropFolder);
            if (textures == null || textures.Length == 0)
            {
                // Silence here is what made this hard to diagnose the first time.
                Debug.LogWarning("No backdrop image found in Resources/" + BackdropFolder
                                 + " — falling back to the flat background colour.");
                return;
            }

            var tex = textures[0];
            _backdropAspect = (float)tex.width / Mathf.Max(1, tex.height);

            var sprite = Sprite.Create(tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                tex.height);   // one unit tall, so scale maps directly to world height

            var go = new GameObject("Backdrop");
            go.transform.SetParent(transform, false);
            _backdrop = go.AddComponent<SpriteRenderer>();
            _backdrop.sprite = sprite;
            _backdrop.color = BackdropTint;
            _backdrop.sortingOrder = -1;   // behind Border(0), Well(1) and the cells(2)

            LayoutBackdrop();
        }

        /// <summary>
        /// Fills the camera view, cropping the overhang. A tall picture in a wide
        /// window loses most of its height, so the crop is anchored on
        /// BackdropFocusY instead of the middle — centring it cuts the face off.
        /// </summary>
        void LayoutBackdrop()
        {
            if (_backdrop == null || cam == null) return;

            float viewH = cam.orthographicSize * 2f;
            float viewW = viewH * ((float)Screen.width / Mathf.Max(1, Screen.height));

            // Cover: the larger scale, so neither axis can leave a gap.
            float scale = Mathf.Max(viewH, viewW / _backdropAspect);
            float spriteH = scale;
            float spriteW = scale * _backdropAspect;

            float camXPos = cam.transform.position.x;
            float camYPos = cam.transform.position.y;

            // Shift so the focus point sits at the centre of the view, then clamp
            // so the picture can never pull away from an edge and show a gap.
            float offsetY = (0.5f - BackdropFocusY) * spriteH;
            float limitY = Mathf.Max(0f, (spriteH - viewH) * 0.5f);
            offsetY = Mathf.Clamp(offsetY, -limitY, limitY);

            float limitX = Mathf.Max(0f, (spriteW - viewW) * 0.5f);

            _backdrop.transform.position =
                new Vector3(camXPos + Mathf.Clamp(0f, -limitX, limitX), camYPos + offsetY, 0f);
            _backdrop.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
