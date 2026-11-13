using UnityEngine;

namespace CiftlikMarket.EditorTools
{
    public static partial class PrototypeBuilder
    {
        /// <summary>Kapalı (inaktif) bir çalışan oluşturur; kilit alanı açılınca belirir.</summary>
        static GameObject WorkerB(string name, Transform parent, Vector3 pos, Worker.Role role, Checkout checkout, Color shirt, string title)
        {
            var root = Empty(name, parent, pos);
            var model = Empty("Model", root.transform, Vector3.zero).transform;
            Prim(PrimitiveType.Capsule, "Body", model, new Vector3(0, 0.55f, 0), new Vector3(0.58f, 0.55f, 0.58f), M("Calisan_" + role, shirt));
            Prim(PrimitiveType.Sphere, "Head", model, new Vector3(0, 1.27f, 0), new Vector3(0.42f, 0.42f, 0.42f), M("Ten", Hex("F1C27D")));
            if (role == Worker.Role.Cashier || role == Worker.Role.DriveThru)
            {
                var capMat = role == Worker.Role.Cashier ? M("KasiyerSapka", Hex("1B5E20")) : M("GorevliSapka", Hex("C62828"));
                Prim(PrimitiveType.Cylinder, "Cap", model, new Vector3(0, 1.48f, 0), new Vector3(0.44f, 0.06f, 0.44f), capMat);
                Box("Visor", model, new Vector3(0, 1.46f, 0.22f), new Vector3(0.3f, 0.03f, 0.18f), capMat);
            }
            else
            {
                Prim(PrimitiveType.Cylinder, "HatBrim", model, new Vector3(0, 1.47f, 0), new Vector3(0.66f, 0.02f, 0.66f), M("Hasir", Hex("D9B26F")));
                Prim(PrimitiveType.Cylinder, "HatTop", model, new Vector3(0, 1.56f, 0), new Vector3(0.36f, 0.08f, 0.36f), M("Hasir", Hex("D9B26F")));
            }

            var stackGo = Empty("Stack", root.transform, new Vector3(0, 0.5f, -0.42f));
            var st = stackGo.AddComponent<ItemContainer>();
            st.capacity = 3;
            st.layout = StackLayout.Column;

            var carrier = root.AddComponent<Carrier>();
            carrier.stack = st;
            carrier.isPlayer = false;
            carrier.canCashier = role == Worker.Role.Cashier;

            var w = root.AddComponent<Worker>();
            w.role = role;
            w.carrier = carrier;
            w.model = model;
            w.checkout = checkout;
            w.idlePoint = pos;

            Label(root.transform, new Vector3(0, 2.0f, 0), title, 0.03f, Dark);
            root.SetActive(false);
            return root;
        }
    }
}
