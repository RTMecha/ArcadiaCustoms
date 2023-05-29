using System;
using System.Collections;
using System.IO;

using UnityEngine;
using UnityEngine.UI;

namespace ArcadiaCustoms.Functions
{
    public class SpriteManager : MonoBehaviour
    {
        public static SpriteManager inst;
        private void Awake()
        {
            inst = this;
        }

        public static void GetSprite(string _path, Image _image, TextureFormat _textureFormat = TextureFormat.ARGB32)
        {
            inst.StartCoroutine(GetSprite(_path, new SpriteLimits(), delegate (Sprite sprite)
            {
                _image.sprite = sprite;
            }, delegate (string onError)
            {
                _image.sprite = ArcadeManager.inst.defaultImage;
            }, _textureFormat));
        }

        public static IEnumerator GetSprite(string _path, SpriteLimits _limits, Action<Sprite> callback, Action<string> onError, TextureFormat _textureFormat = TextureFormat.ARGB32)
        {
            yield return inst.StartCoroutine(LoadImageFileRaw(_path, delegate (Sprite _texture)
            {
                if (((float)_texture.texture.width > _limits.size.x && _limits.size.x > 0f) || ((float)_texture.texture.height > _limits.size.y && _limits.size.y > 0f))
                {
                    onError(_path);
                    return;
                }
                callback(_texture);
            }, delegate (string error)
            {
                onError(_path);
            }, _textureFormat));
            yield break;
        }

        public static IEnumerator LoadImageFileRaw(string _filepath, Action<Sprite> callback, Action<string> onError, TextureFormat _textureFormat = TextureFormat.ARGB32)
        {
            if (!File.Exists(_filepath))
            {
                onError(_filepath);
            }
            else
            {
                Texture2D tex = new Texture2D(256, 256, _textureFormat, false);
                tex.requestedMipmapLevel = 3;
                Sprite sprite;
                using (WWW www = new WWW("file://" + _filepath))
                {
                    while (!www.isDone)
                        yield return (object)null;
                    www.LoadImageIntoTexture(tex);
                    tex.Apply(true);
                    sprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, (float)tex.width, (float)tex.height), new Vector2(0.5f, 0.5f), 100f);
                }
                callback(sprite);
                tex = (Texture2D)null;
            }
        }

        public class SpriteLimits
        {
            public SpriteLimits()
            {
            }

            public SpriteLimits(Vector2 _size)
            {
                size = _size;
            }

            public Vector2 size = Vector2.zero;
        }
    }
}
