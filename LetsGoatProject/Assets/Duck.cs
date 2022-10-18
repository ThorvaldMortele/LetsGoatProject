using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Duck : NetworkBehaviour
{
    [SerializeField]
    private BezierSpline _spline;
    [SerializeField]
    private GameObject _visualParent;

    [SerializeField] private float _splineProgress;
    [SerializeField]
    private float _loopTime = 15f;

    public override void FixedUpdateNetwork()
    {
        MoveSpline();
    }

    private void MoveSpline()
    {
        _splineProgress += Runner.DeltaTime / _loopTime;
        if (_splineProgress > 1)
        {
            _splineProgress -= 1;
        }
        _visualParent.transform.position = _spline.GetPoint(_splineProgress);

        Vector3 rotation = new Vector3(0, 360 * _splineProgress, 0);

        _visualParent.transform.rotation = Quaternion.Euler(180, -rotation.y, _visualParent.transform.rotation.z);
    }
}
