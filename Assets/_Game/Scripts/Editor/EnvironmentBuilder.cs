using UnityEngine;

namespace BeforeItWasYou.EditorTools
{
    /// <summary>
    /// Compone el entorno del episodio (calle, parcela, casa, patio, cobertizo) y devuelve los puntos clave.
    /// Coordenadas en metros; la calle queda al sur (z negativa) y el patio al norte (Docs/Proyecto/PLANO_TECNICO.md).
    /// </summary>
    public static class EnvironmentBuilder
    {
        public sealed class Result
        {
            public Transform Root;
            public Transform ArrivalSpawn;
            public Transform MenuFrom;
            public Transform MenuTo;
        }

        public static Result Build(Transform sun)
        {
            var root = new GameObject("Environment").transform;
            var markers = new GameObject("Markers").transform;
            markers.SetParent(root, false);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(root, false);
            ground.transform.localScale = new Vector3(8f, 1f, 8f);

            return new Result
            {
                Root = root,
                ArrivalSpawn = Marker(markers, "Spawn_Arrival", new Vector3(1.0f, 0.02f, 5.0f), 55f),
                MenuFrom = Marker(markers, "Menu_From", ArrivalSceneBuilder.MenuFrom, ArrivalSceneBuilder.MenuFromEuler),
                MenuTo = Marker(markers, "Menu_To", ArrivalSceneBuilder.MenuTo, ArrivalSceneBuilder.MenuToEuler),
            };
        }

        private static Transform Marker(Transform parent, string name, Vector3 position, float yaw) =>
            Marker(parent, name, position, new Vector3(0f, yaw, 0f));

        private static Transform Marker(Transform parent, string name, Vector3 position, Vector3 euler)
        {
            var marker = new GameObject(name).transform;
            marker.SetParent(parent, false);
            marker.SetPositionAndRotation(position, Quaternion.Euler(euler));
            return marker;
        }
    }
}
