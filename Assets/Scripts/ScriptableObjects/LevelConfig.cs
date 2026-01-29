using UnityEngine;

namespace ScriptableObjects
{
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "ScriptableObjects/LevelConfig", order = 1)]
    public class LevelConfig : ScriptableObject
    {
        [Header("Animation Curve")] public AnimationCurve animationCurve;
        public int maxLevel;
        public int maxRequiredExp;

        public int GetRequiredExp(int level)
        {
            //InverseLerp: min, max => value based on current value. example: min=0, maxlevel=100, level=50 -> 0.5
            //Evaluate: [0-1] => returns value Y based on Graph (for x=0.5 in this example)
            int requiredExperience = Mathf.RoundToInt(animationCurve.Evaluate(Mathf.InverseLerp(0,maxLevel,level))*maxRequiredExp);
            return requiredExperience;
        }
    }
}