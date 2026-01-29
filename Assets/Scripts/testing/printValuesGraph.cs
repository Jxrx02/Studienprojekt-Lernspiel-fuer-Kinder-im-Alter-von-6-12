
using TowerDefense;

namespace testing
{
    using UnityEngine.Serialization;
    using System.Collections.Generic;
    using UnityEngine;

    public class PrintValuesGraph : MonoBehaviour
    {
        public AnimationCurve spawnPlot = new AnimationCurve(); // Kurve für Gegner
        public AnimationCurve playerHealthPlot = new AnimationCurve(); // Kurve für Spieler-Gesundheit
        private float startTime; 

        void Start()
        {
            startTime = Time.time; 
        }

        void Update()
        {
            AddSpawnKey(Time.time, TowerHeroManager.instance.enemies.Count);
            AddPlayerHealthKey(Time.time, LevelManager.instance.cur_health);
        }

        public void AddSpawnKey(float time, int enemyCount)
        {
            float elapsedTime = time - startTime;
            spawnPlot.AddKey(elapsedTime, enemyCount);
        }

        public void AddPlayerHealthKey(float time, float health)
        {
            float elapsedTime = time - startTime;
            playerHealthPlot.AddKey(elapsedTime, health);
        }

        void OnDrawGizmos()
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one); // Position anpassen

            DrawGraph(spawnPlot, Color.green); // Gegner-Kurve
            DrawGraph(playerHealthPlot, Color.red); // Spieler-Gesundheit-Kurve
        }

        void DrawGraph(AnimationCurve curve, Color color)
        {
            Gizmos.color = color;

            for (int i = 1; i < curve.keys.Length; i++)
            {
                var prevKey = curve.keys[i - 1];
                var currentKey = curve.keys[i];

                // Zeitachse bleibt gleich, nur die Werte unterscheiden sich
                Vector3 prevPoint = new Vector3(prevKey.time, prevKey.value, 0);
                Vector3 currentPoint = new Vector3(currentKey.time, currentKey.value, 0);

                Gizmos.DrawLine(prevPoint, currentPoint);
            }
        }
    }


}