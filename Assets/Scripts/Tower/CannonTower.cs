using System.Collections;
using UnityEngine;

namespace TowerDefense
{
    public class CannonTower : Tower
    {

        public override void Attack((GameObject, int) _target)
        {
            if (isAttacking) return;

            target = _target;
            if (target.Item1 == null) return;

            if (IsObjectInRange(target.Item1))
            {
                isAttacking = true;
                StartCoroutine(BaseAttackCoroutine(Shoot));
            }
        }
        
        protected override void UpdateLookDirection(Vector3 targetPos)
        {
            Quaternion originalCanvasRotation = GetComponentInChildren<Canvas>().transform.rotation;

            if (targetPos.x > transform.position.x)
                transform.rotation = new Quaternion(0, 180, 0, 1);
            else
                transform.rotation = new Quaternion(0, 0, 0, 1);
            
            GetComponentInChildren<Canvas>().transform.rotation = originalCanvasRotation;

        }


    }
}