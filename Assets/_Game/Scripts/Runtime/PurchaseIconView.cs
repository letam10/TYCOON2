using UnityEngine;
using UnityEngine.Rendering;
namespace Tycoon
{
    public sealed class PurchaseIconView : MonoBehaviour
    {
        SpriteRenderer display;
        Texture2D texture;
        Sprite sprite;
        string shown;
        public void SetUpgrade(UpgradeDefinition upgrade)
        {
            if(shown==upgrade.id)return;
            shown=upgrade.id;
            if(!display)
            {
                display=gameObject.AddComponent<SpriteRenderer>();
                display.shadowCastingMode=ShadowCastingMode.Off;
                display.receiveShadows=false;
            }
            ReleaseImage();
            texture=new Texture2D(DrawnPurchaseIcons.Size,DrawnPurchaseIcons.Size,TextureFormat.RGBA32,false)
            {
                name="Purchase_"+shown,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp
            }
            ;
            texture.SetPixels32(DrawnPurchaseIcons.Draw(upgrade));
            sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),DrawnPurchaseIcons.Size,0,SpriteMeshType.FullRect);
            texture.Apply(false,true);
            sprite.name="Purchase_"+shown;
            display.sprite=sprite;
            if(Camera.main)transform.rotation=Camera.main.transform.rotation;
        }
        void LateUpdate()
        {
            if(Camera.main)transform.rotation=Camera.main.transform.rotation;
        }
        void ReleaseImage()
        {
            if(display)display.sprite=null;
            if(sprite)
            {
                if(Application.isPlaying)Destroy(sprite);
                else DestroyImmediate(sprite);
            }
            if(texture)
            {
                if(Application.isPlaying)Destroy(texture);
                else DestroyImmediate(texture);
            }
        }
        void OnDestroy()=>ReleaseImage();
    }
}
