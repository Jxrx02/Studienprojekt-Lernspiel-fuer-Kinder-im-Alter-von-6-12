using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using TowerDefense.GridMovement;

namespace TowerDefense
{
    public class Wall : Tower
    {
        protected bool isBuilt = false;
        public int HP => statHealthPoints;

        protected override void Awake()
        {
            base.Awake();
            
            isBuilt = false;
        }

        public override void Attack((GameObject, int) _target)
        {
        }
        
    }
}