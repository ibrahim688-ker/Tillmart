using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CiftlikMarket
{
    /// <summary>
    /// Depo açıldığında Hasan Amca tekrar gelir ve toptancı sistemini anlatır (GDD 3.12):
    /// toptancıdan sipariş, kamyonet, depo, depocu ve drive-thru'nun depodan beslenmesi.
    /// Bir kez oynar (cmg_story_depo). İlk öğretici bitmeden ya da başka bir pencere açıkken başlamaz.
    /// Konuşma kutusu ilk öğreticiyle aynı görünümdedir.
    /// </summary>
    public class DepoStory : MonoBehaviour
    {
        public static bool Blocking { get; private set; }
        const string DoneKey = "cmg_story_depo";

        class Line { public string text; public Vector3? focus; }
        readonly List<Line> lines = new List<Line>();
        int lineIndex;
        string fullText = "";
        float typed, waitTimer, waveTimer;
        bool running, walking, leaving;

        Carrier player;
        GameObject npc;
        Transform npcArm;
        NavMover npcMover;
        CameraFollow camFollow;
        Transform focus;
        float walkTime;

        Canvas canvas;
        Text boxText;
        RectTransform tapHint;

        static bool OthersOpen => OfficeUI.IsOpen || OfflineEarnings.IsOpen || AdService.IsShowing || BranchManager.IsConfirmOpen
                                  || Tutorial.Blocking || Tutorial.Active || DailyReward.IsOpen || Quests.IsOpen || Wholesale.IsOpen;

        void Update()
        {
            if (!running)
            {
                if (PlayerPrefs.GetInt(DoneKey, 0) == 1 || PlayerPrefs.GetInt("cmg_tut_done", 0) != 1) return;
                var dock = DeliveryDock.I;
                if (dock == null || !dock.isActiveAndEnabled || OthersOpen) { waitTimer = 0f; return; }
                waitTimer += Time.deltaTime;
                if (waitTimer < 1.5f) return;   // oda açılış animasyonu bitsin
                Begin();
                return;
            }

            if (walking) UpdateWalk();
            else if (!leaving) FacePlayer();
            if (canvas != null && canvas.gameObject.activeSelf) UpdateText();
        }

        // ---------------- akış ----------------

        void Begin()
        {
            foreach (var c in Carrier.All) if (c.isPlayer) player = c;
            var world = GameObject.Find("World");
            var t = world != null ? world.transform.Find("Hasan_Amca") : null;
            if (player == null || t == null) { PlayerPrefs.SetInt(DoneKey, 1); return; }
            running = true;
            Blocking = true;
            camFollow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;

            npc = t.gameObject;
            var old = npc.GetComponent<NpcLeaver>();
            if (old != null) Destroy(old);
            npcArm = npc.transform.Find("Model_v2/ArmR");
            // marketin kapısından girer, oyuncunun yanına yürür
            var cs = FindAnyObjectByType<CustomerSpawner>();
            Vector3 start = cs != null && cs.entryPoint ? cs.entryPoint.position : player.transform.position + new Vector3(4f, 0f, -2f);
            npc.transform.position = start;
            npc.SetActive(true);
            npcMover = null;
            walking = true;
            walkTime = 0f;
            AudioFx.Play(Sfx.Popup, 0.7f);
            if (camFollow) camFollow.target = player.transform;
        }

        void UpdateWalk()
        {
            walkTime += Time.deltaTime;
            var pp = player.transform.position;
            var d = pp - npc.transform.position; d.y = 0f;
            if (npcMover == null) npcMover = new NavMover(npc, 3.6f, 0.3f, 60);
            bool arrived = d.magnitude < 2.6f;
            if (!arrived) npcMover.MoveTo(pp - d.normalized * 2.0f, 3.6f, 0.4f);
            if (arrived || walkTime > 9f)
            {
                if (!arrived) npc.transform.position = pp + new Vector3(1.6f, 0f, 0.2f);   // takıldıysa yanına gelsin
                walking = false;
                npcMover.MoveTo(npc.transform.position, 3.6f, 5f);
                StartTalk();
            }
        }

        void FacePlayer()
        {
            if (npc == null || player == null) return;
            var d = player.transform.position - npc.transform.position; d.y = 0f;
            if (d.sqrMagnitude > 0.01f)
                npc.transform.rotation = Quaternion.Slerp(npc.transform.rotation, Quaternion.LookRotation(d), 1f - Mathf.Exp(-6f * Time.deltaTime));
            waveTimer -= Time.deltaTime;
        }

        void LateUpdate()
        {
            if (!running || npcArm == null || waveTimer <= 0f) return;
            float w = Mathf.Sin(waveTimer * 14f) * 35f * Mathf.Clamp01(waveTimer);
            npcArm.localRotation = Quaternion.Euler(0f, 0f, 150f + w);
        }

        void StartTalk()
        {
            lines.Clear();
            var p = player.transform.position;
            var dock = DeliveryDock.I;
            Shelf bakkal = null;
            foreach (var s in Shelf.All) if (Wholesale.IsWholesale(s.type)) { bakkal = s; break; }
            var dt = FindAnyObjectByType<DriveThru>();

            Add("Yeğenim! Bakkal reyonunu açmışsın, maşallah. Gel, sana bir şey göstereyim.", (p + npc.transform.position) * 0.5f);
            if (bakkal != null) Add("Makarna, şeker, çay gibi ürünleri çiftlikte yetiştiremeyiz. Bunları toptancıdan alacağız.", bakkal.transform.position + new Vector3(2f, 0f, -2f));
            Add("Tabletini aç, ana sayfadaki turuncu kamyonet butonuna bas. Toptancı sayfası açılır.", p);
            Add("Üç toptancı var. Mahalle deposu hemen getirir ama pahalıdır. Şehir toptancısıyla hal daha ucuzdur, ama biraz beklersin.", p);
            if (dock != null) Add("Sipariş verince kamyonet arkadaki yoldan gelir, malları bu depoya bırakır.", dock.transform.position + new Vector3(0f, 0f, -1f));
            if (dock != null) Add("Depodaki malları raflara sen taşıyabilirsin. İstersen tabletten bir depocu tut, o taşısın.", dock.transform.position);
            if (dt != null) Add("Drive-thru'dan bakkal ürünü isteyen olursa görevli de gelip buradan alır.", dt.transform.position + new Vector3(1.5f, 0f, 0f));
            Add("Haydi, ilk siparişini ver bakalım. Kolay gelsin yeğenim!", p);
            lineIndex = 0;
            if (canvas == null) BuildUI();
            canvas.gameObject.SetActive(true);
            ShowLine();
            Haptics.Light();
        }

        void Add(string text, Vector3? at) { lines.Add(new Line { text = text, focus = at }); }

        void ShowLine()
        {
            var l = lines[lineIndex];
            fullText = l.text;
            typed = 0f;
            waveTimer = 1.2f;
            if (l.focus.HasValue && camFollow)
            {
                if (focus == null) focus = new GameObject("DepoStoryFocus").transform;
                focus.position = l.focus.Value;
                camFollow.target = focus;
            }
        }

        void UpdateText()
        {
            typed = Mathf.Min(fullText.Length, typed + Time.deltaTime * 45f);
            int n = Mathf.FloorToInt(typed);
            boxText.text = n >= fullText.Length ? fullText : fullText.Substring(0, n);
            bool done = n >= fullText.Length;
            tapHint.gameObject.SetActive(done);
            if (done) tapHint.anchoredPosition = new Vector2(-46f, 40f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 5f)) * 8f);
        }

        void OnTap()
        {
            if (!running || walking) return;
            if (typed < fullText.Length) { typed = fullText.Length; return; }
            AudioFx.Play(Sfx.Tap, 0.6f);
            lineIndex++;
            if (lineIndex < lines.Count) { ShowLine(); return; }
            Finish();
        }

        void Finish()
        {
            PlayerPrefs.SetInt(DoneKey, 1);
            PlayerPrefs.Save();
            Blocking = false;
            if (canvas) canvas.gameObject.SetActive(false);
            if (camFollow && player) camFollow.target = player.transform;
            if (focus) Destroy(focus.gameObject);
            // Hasan Amca kapıdan çıkıp yola yürür
            if (npc)
            {
                leaving = true;
                var keeper = npc.AddComponent<NpcLeaver>();
                var cs = FindAnyObjectByType<CustomerSpawner>();
                if (cs != null && cs.entryPoint && cs.exitDoor && cs.exitPoint)
                    keeper.route = new[] { cs.entryPoint.position, cs.exitDoor.position, cs.exitPoint.position };
                else keeper.route = new[] { new Vector3(-9f, 0f, -18f) };
            }
            running = false;
            AudioFx.Play(Sfx.LevelUp, 0.7f);
        }

        // ---------------- arayüz (ilk öğreticiyle aynı görünüm) ----------------

        void BuildUI()
        {
            canvas = UIKit.MakeCanvas("DepoStoryCanvas", transform, 36);
            var root = canvas.transform;

            var catcher = UIKit.Img("TapCatcher", root, null, new Color(0f, 0f, 0f, 0.001f));
            catcher.raycastTarget = true;
            UIKit.Stretch(catcher.rectTransform);
            catcher.gameObject.AddComponent<Button>().onClick.AddListener(OnTap);

            var boxRt = UIKit.Node("Dialog", root);
            UIKit.Place(boxRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(20f, -250f), new Vector2(960f, 262f));
            var box = UIKit.MakeChunk("Box", boxRt, UIKit.Cream, UIKit.CreamLip, 52f, 6f, 12f);
            UIKit.Place(box.Rect, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(70f, 0f), new Vector2(890f, 232f));
            var face = box.Face.transform;
            boxText = UIKit.Label("Text", face, "", 35, UIKit.TextDark, TextAnchor.MiddleLeft, false, false);
            boxText.lineSpacing = 0.95f;
            UIKit.Stretch(boxText.rectTransform, 148f, 10f, 64f, 42f);

            var tag = UIKit.MakeChunk("Name", boxRt, UIKit.Hex("5E7A4A"), UIKit.Hex("3E5530"), 30f, 5f, 7f, false);
            UIKit.Place(tag.Rect, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(200f, -32f), new Vector2(250f, 62f));
            UIKit.Stretch(UIKit.Label("T", tag.Face.transform, "HASAN AMCA", 30, Color.white, TextAnchor.MiddleCenter, true).rectTransform, 0, -3, 0, 0);

            var po = UIKit.Img("Portrait", boxRt, UIKit.Circle, UIKit.Ink);
            UIKit.Place(po.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 10f), new Vector2(200f, 200f));
            var pf = UIKit.Img("Bg", po.transform, UIKit.Circle, UIKit.Hex("BFEAFF"));
            UIKit.Stretch(pf.rectTransform, 8, 8, 8, 8);
            var mask = pf.gameObject.AddComponent<Mask>(); mask.showMaskGraphic = true;
            var face2 = UIKit.Img("Face", pf.transform, UIKit.Icon("hasan_amca"), Color.white);
            face2.preserveAspect = true;
            UIKit.Stretch(face2.rectTransform, -6, -14, -6, 2);

            var skip = UIKit.MakeButton("Skip", boxRt, UIKit.Gray, UIKit.GrayLip, 32f, () => { if (running && !walking) Finish(); }, 5f, 8f);
            UIKit.Place(skip.Rect, new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-28f, -32f), new Vector2(112f, 64f));
            UIKit.Stretch(UIKit.Label("T", skip.Face.transform, "GEÇ", 28, Color.white, TextAnchor.MiddleCenter, true).rectTransform, 0, -3, 0, 0);

            var th = UIKit.Img("TapHint", face, UIKit.PlayTri, UIKit.Hex("FF8A3D"));
            tapHint = th.rectTransform;
            UIKit.Place(tapHint, new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-46f, 40f), new Vector2(46f, 46f));
            tapHint.localEulerAngles = new Vector3(0, 0, -90f);

            canvas.gameObject.SetActive(false);
        }
    }
}
