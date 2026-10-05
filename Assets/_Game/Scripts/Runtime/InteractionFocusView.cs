using UnityEngine;
namespace Tycoon
{
    // Viền vật thể đang tương tác chỉ là view, không tạo ô thao tác hoặc sửa state.
    public sealed class InteractionFocusView:MonoBehaviour
    {
        LineRenderer line;
        void Start(){line=gameObject.AddComponent<LineRenderer>();line.sharedMaterial=Art.Material("#E8D882");line.widthMultiplier=.045f;line.loop=true;line.positionCount=4;line.useWorldSpace=true;}
        void LateUpdate()
        {
            var target=GameSession.Instance?.Player?.ActiveInteraction as ProximityTarget;
            if(!line)return;line.enabled=target!=null;if(target==null)return;
            float half=target.Cash?.55f:target.Target is ProductionStation p&&!p.Animal?2.15f:target.Target is CargoDock?.65f:1.35f;
            var center=target.Center+Vector3.up*.07f;
            line.SetPositions(new[]{center+new Vector3(-half,0,-half),center+new Vector3(half,0,-half),center+new Vector3(half,0,half),center+new Vector3(-half,0,half)});
        }
    }
}
