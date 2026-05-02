using UnityEngine;
#pragma warning disable CS0618
namespace YizziCamModV2.Comps
{
    class LeftGrabTrigger : MonoBehaviour
    {
        void Start()
        {
            gameObject.layer = 18;
        }
        void OnTriggerStay(Collider col)
        {
            bool holding = InputManager.instance.LeftGrip || InputManager.instance.LeftTrigger;
            if (col.name.Contains("Left"))
            {
                if (holding & (!CameraController.Instance.fpv || CameraController.Instance.camDisconnect))
                {
                    CameraController.Instance.CameraTablet.transform.parent = CameraController.Instance.LeftHandGO.transform;
                    if (CameraController.Instance.fp) { CameraController.Instance.fp = false; }
                }
            }
            if (!holding & CameraController.Instance.CameraTablet.transform.parent == CameraController.Instance.LeftHandGO.transform)
            {
                CameraController.Instance.CameraTablet.transform.parent = null;
            }
        }
    }
}
