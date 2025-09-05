using UnityEngine;

public class HollowOutMaskTest : MonoBehaviour
{
    public RectTransform target;
    public HollowOutMask hollow;
    private void OnEnable()
    {
        hollow.SetTarget(target);
    }
}

