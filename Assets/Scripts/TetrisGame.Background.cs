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

        // Dimmed hard: the board sits on top of this and has to stay readable.
        static readonly Color BackdropTint = new Color(0.38f, 0.38f, 0.42f, 1f);

        SpriteRenderer _backdrop;
        float _backdropAspect = 1f;

        void SetupBackdrop()
        {
            var textures = Resources.LoadAll<Texture2D>(BackdropFolder);
            if (textures == null || textures.Length == 0) return;   // no picture, keep the flat colour

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
        /// Sizes the backdrop to cover the camera view whatever the aspect ratio,
        /// cropping the overhang rather than stretching the picture.
        /// </summary>
        void LayoutBackdrop()
        {
            if (_backdrop == null || cam == null) return;

            float viewH = cam.orthographicSize * 2f;
            float viewW = viewH * ((float)Screen.width / Mathf.Max(1, Screen.height));

            // Cover: take whichever scale fills both axes, so no gap can show.
            float scale = Mathf.Max(viewH, viewW / _backdropAspect);

            _backdrop.transform.position = new Vector3(cam.transform.position.x,
                                                       cam.transform.position.y, 0f);
            _backdrop.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
