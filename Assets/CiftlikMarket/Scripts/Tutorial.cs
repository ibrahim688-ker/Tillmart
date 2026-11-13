using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CiftlikMarket
{
    /// <summary>
    /// Hikâyeli ilk oyun rehberi (FTUE). Çiftlik ve market oyuncuya babasından kalmıştır; babasının can dostu
    /// Hasan Amca karşılar ("Yeğenim, hoş geldin!"), kamerayla ortamı tanıtır, sonra adım adım ilk döngüyü öğretir:
    /// yürü → saman topla → ineğe ver → sütü al → rafa diz → kasada dur → parayı topla → mısır tarlasını aç.
    /// Yalnızca ilk şubede ve sıfırdan başlayan oyunda çalışır; ilerleme kalıcıdır, "GEÇ" ile atlanabilir.
    /// </summary>
    [DefaultExecutionOrder(100)]   // el sallama, CharacterAnimator'dan (LateUpdate) sonra uygulansın
    public class Tutorial : MonoBehaviour
    {
        const string DoneKey = "cmg_tut_done";
        const string StepKey = "cmg_tut_step";
        const string Npc = "HASAN AMCA";

        enum Step { Intro, Move, Hay, Feed, Milk, Shelf, Checkout, Money, Unlock, Tablet, Upgrade, Outro }
        const int Total = 10;  // ekranda gösterilen adım sayısı (Move..Upgrade)

        public static bool Active { get; private set; }
        /// <summary>Konuşma sırasında joystick çalışmasın (UIBlock okur).</summary>
        public static bool Blocking { get; private set; }
        /// <summary>Hasan Amca ortamı tanıtırken müşteri gelmesin (CustomerSpawner okur).</summary>
        public static bool HoldCustomers => Active && inst != null && inst.step == Step.Intro;
        static Tutorial inst;

        Step step;
        Carrier player;
        ResourceField hayField;
        Converter cow;
        Shelf milkShelf;
        Checkout checkout;
        MoneyPile pile;
        UnlockZone cornZone, cornShelfZone;
        Upgrade capUpgrade;
        int capLevelAtStart;
        Vector3 startPos;
        float stepAge;

        // konuşma
        struct Line { public string text; public Vector3? focus; }
        readonly List<Line> lines = new List<Line>();
        int lineIndex;
        float typed;          // daktilo efekti: gösterilen karakter sayısı
        string fullText = "";

        // Hasan Amca
        GameObject npc;
        Transform npcArm;
        NavMover npcMover;
        float waveTimer;
        bool npcLeaving;
        Vector3 npcExit = new Vector3(-9f, 0f, -18f);

        // kamera
        CameraFollow camFollow;
        Transform focus;

        // görseller
        Transform arrow;
        Canvas canvas;
        RectTransform boxRt, pointerRt, pointerArrow, joyRt, joyKnob, tapHint;
        Text boxText, countText;
        GameObject countPill, tapCatcher;

        // ---------------- kurulum ----------------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Hook()
        {
            SceneManager.sceneLoaded -= OnLoaded;
            SceneManager.sceneLoaded += OnLoaded;
        }

        static void OnLoaded(Scene s, LoadSceneMode m)
        {
            if (PlayerPrefs.GetInt(DoneKey, 0) == 1) return;
            if (FindAnyObjectByType<Tutorial>() != null) return;
            new GameObject("Tutorial").AddComponent<Tutorial>();
        }

        void Start()
        {
            if (PlayerPrefs.GetInt("cmg_branch", 1) != 1 || GameManager.I == null) { Destroy(gameObject); return; }

            step = (Step)PlayerPrefs.GetInt(StepKey, 0);
            // rehber eklenmeden önce oynamaya başlamış oyuncular: atla
            if (step == Step.Intro && (GameManager.I.Money > 0 || PlayerPrefs.GetString("cm_unlocked", "").Length > 0)) { Complete(); return; }

            foreach (var c in Carrier.All) if (c.isPlayer) player = c;
            foreach (var f in ResourceField.All) if (f.produces == ItemType.Saman) { hayField = f; break; }
            foreach (var c in Converter.All) if (c.output == ItemType.Sut && c.input == ItemType.Saman) { cow = c; break; }
            foreach (var s in Shelf.All) if (s.type == ItemType.Sut) { milkShelf = s; break; }
            checkout = FindAnyObjectByType<Checkout>();
            pile = checkout != null ? checkout.moneyPile : FindAnyObjectByType<MoneyPile>();
            foreach (var z in FindObjectsByType<UnlockZone>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (z.id == "tarla_misir") cornZone = z;
                if (z.id == "raf_misir") cornShelfZone = z;
            }

            foreach (var u in FindObjectsByType<Upgrade>(FindObjectsSortMode.None)) if (u.kind == UpgradeKind.PlayerCapacity) capUpgrade = u;

            if (player == null || hayField == null || cow == null || milkShelf == null || checkout == null || pile == null || cornZone == null)
            { Debug.LogWarning("[Tutorial] hedefler bulunamadı, rehber atlandı"); Complete(); return; }

            inst = this;
            startPos = player.transform.position;
            camFollow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
            Active = true;
            SetupNpc();
            BuildArrow();
            BuildUI();
            Enter(step);
        }

        void OnDestroy()
        {
            Active = false;
            Blocking = false;
            if (camFollow != null && player != null) camFollow.target = player.transform;
            if (focus) Destroy(focus.gameObject);
        }

        void SetupNpc()
        {
            var world = GameObject.Find("World");
            var t = world != null ? world.transform.Find("Hasan_Amca") : null;
            if (t == null) return;
            npc = t.gameObject;
            npc.SetActive(true);
            // oyuncunun yanında başlasın (yarıda kalan rehberde de)
            var p = player.transform.position + new Vector3(1.6f, 0f, 0.2f);
            npc.transform.position = p;
            npc.transform.rotation = Quaternion.LookRotation(player.transform.position - p);
            npcArm = npc.transform.Find("Model_v2/ArmR");
        }

        // ---------------- akış ----------------

        void Update()
        {
            if (!Active) return;
            stepAge += Time.deltaTime;
            var st = player.stack;
            int hay = st.CountOf(ItemType.Saman), milk = st.CountOf(ItemType.Sut);

            switch (step)
            {
                case Step.Intro:
                case Step.Outro:
                    UpdateDialogue();
                    break;
                case Step.Move:
                    if ((player.transform.position - startPos).sqrMagnitude > 2.25f) Next();
                    break;
                case Step.Hay:
                    if (hay >= Mathf.Min(4, st.capacity) || (hay > 0 && hayField.GrownCount == 0)) Next();
                    break;
                case Step.Feed:
                    if (hay == 0 && (cow.inputPile.Count > 0 || cow.outputTray.Count > 0)) Next();
                    else if (hay == 0 && cow.inputPile.Count == 0 && cow.outputTray.Count == 0) Go(Step.Hay);
                    break;
                case Step.Milk:
                    if (milk > 0 && ((cow.inputPile.Count == 0 && cow.outputTray.Count == 0) || st.IsFull)) Next();
                    else if (milk == 0 && cow.inputPile.Count == 0 && cow.outputTray.Count == 0 && milkShelf.container.Count == 0) Go(Step.Hay);
                    break;
                case Step.Shelf:
                    if (milk == 0 && (milkShelf.container.Count > 0 || pile.amount > 0)) Next();
                    else if (milk == 0) Go(Step.Hay);
                    break;
                case Step.Checkout:
                    if (pile.amount > 0) Next();
                    break;
                case Step.Money:
                    if (pile.amount == 0) Next();
                    break;
                case Step.Unlock:
                    if (GameManager.I.IsUnlocked("tarla_misir")) Go(capUpgrade != null && OfficeUI.I != null ? Step.Tablet : Step.Outro);
                    break;
                case Step.Tablet:
                    if (OfficeUI.IsOpen) Next();
                    break;
                case Step.Upgrade:
                    if (capUpgrade.Level > capLevelAtStart) { OfficeUI.I.CloseTablet(); Next(); }
                    break;
            }
            if (!Active) return;

            if (step != Step.Intro && step != Step.Outro) SetText(StepText(hay, milk));
            UpdateTyping();
            UpdateNpc();
            UpdateVisuals();
        }

        void Next() { Go(step + 1); }

        void Go(Step s)
        {
            if (s == step) return;
            step = s;
            PlayerPrefs.SetInt(StepKey, (int)s);
            PlayerPrefs.Save();
            Enter(s);
            if (s != Step.Move && s != Step.Intro) { AudioFx.Play(Sfx.Tap, 0.7f); Haptics.Light(); }
        }

        void Enter(Step s)
        {
            stepAge = 0f;
            waveTimer = 1.4f;
            if (boxRt)
            {
                var punch = boxRt.GetComponent<UIPunch>();
                if (punch == null) punch = boxRt.gameObject.AddComponent<UIPunch>();
                punch.Punch(0.1f);
            }

            if (s == Step.Intro) StartIntro();
            else if (s == Step.Outro) StartOutro();
            else if (s == Step.Upgrade)
            {
                Blocking = false;
                capLevelAtStart = capUpgrade.Level;
                // Hasan Amca harçlık verir: ilk yükseltmenin parası tamamlansın
                int lack = capUpgrade.Cost - GameManager.I.Money;
                if (lack > 0) { GameManager.I.AddMoney(lack); AudioFx.Play(Sfx.Coin); gifted = true; }
            }
            else
            {
                Blocking = false;
                if (camFollow) camFollow.target = player.transform;
            }
        }

        void Complete()
        {
            Active = false;
            Blocking = false;
            PlayerPrefs.SetInt(DoneKey, 1);
            PlayerPrefs.DeleteKey(StepKey);
            PlayerPrefs.Save();
            if (canvas) Destroy(canvas.gameObject);
            if (arrow) Destroy(arrow.gameObject);
            if (npc && !npcLeaving) npc.SetActive(false);
            Destroy(gameObject);
        }

        void Skip()
        {
            AudioFx.Play(Sfx.Tap);
            if (npc) npc.SetActive(false);
            Complete();
        }

        // ---------------- konuşmalar ----------------

        void StartIntro()
        {
            Blocking = true;
            lines.Clear();
            var mid = (player.transform.position + npc_Pos()) * 0.5f;
            Add("Yeğenim, hoş geldin! Ben Hasan Amcan, rahmetli babanın can dostuyum.", mid);
            Add("Baban bu çiftliği ve marketi sana bıraktı. Biraz bakımsız kaldı ama birlikte toparlarız.", mid);
            Add("Burası babanın marketi. Köylüler alışverişe buraya gelir. Raflar boş kalmasın!", milkShelf.transform.position + new Vector3(0.5f, 0f, -4f));
            Add("Arkada da çiftlik var: saman tarlası ve Sarıkız. Sarıkız samanı yer, karşılığında süt verir.", (hayField.transform.position + cow.transform.position) * 0.5f);
            Add("Müşteriler ödemeyi kasada yapar. Kasada biri durmazsa kimse ödeme yapamaz.", checkout.transform.position);
            Add("Para kazandıkça yeni yerler açarsın. İlk hedefimiz şu mısır tarlası.", cornZone.transform.position);
            Add("Hadi bakalım, önce bir ısın: parmağını ekranda sürükle, yürü!", player.transform.position);
            lineIndex = 0;
            ShowLine();
        }

        void StartOutro()
        {
            Blocking = true;
            lines.Clear();
            var corn = cornZone.unlockTargets != null && cornZone.unlockTargets.Length > 0 && cornZone.unlockTargets[0] ? cornZone.unlockTargets[0].transform.position : cornZone.transform.position;
            Add("Bereketli olsun! Mısırı tavuklara yem yaparsın, istersen markette rafa dizip de satarsın.", corn);
            if (cornShelfZone != null) Add("Bak, markette mısır rafının yeri de hazır. Parayı biriktirince onu da açarsın.", cornShelfZone.transform.position);
            Add("Babanın emeği sana emanet. Ben arada uğrarım. Kolay gelsin yeğenim!", player.transform.position);
            lineIndex = 0;
            ShowLine();
            AudioFx.Play(Sfx.LevelUp, 0.8f);
            Haptics.Medium();
        }

        Vector3 npc_Pos() => npc != null ? npc.transform.position : player.transform.position;

        void Add(string text, Vector3? at) { lines.Add(new Line { text = text, focus = at }); }

        void ShowLine()
        {
            var l = lines[lineIndex];
            SetText(l.text);
            typed = 0f;
            waveTimer = 1.2f;
            if (l.focus.HasValue && camFollow)
            {
                if (focus == null) focus = new GameObject("TutorialFocus").transform;
                focus.position = l.focus.Value;
                camFollow.target = focus;
            }
        }

        void UpdateDialogue()
        {
            // dokunma tapCatcher butonundan gelir (OnTap)
        }

        void OnTap()
        {
            if (step != Step.Intro && step != Step.Outro) return;
            if (typed < fullText.Length) { typed = fullText.Length; return; }   // önce yazının tamamını göster
            AudioFx.Play(Sfx.Tap, 0.6f);
            lineIndex++;
            if (lineIndex < lines.Count) { ShowLine(); return; }
            if (step == Step.Intro) Go(Step.Move);
            else FinishOutro();
        }

        void FinishOutro()
        {
            Blocking = false;
            if (camFollow) camFollow.target = player.transform;
            // Hasan Amca yola doğru yürüyüp gider
            if (npc)
            {
                npcLeaving = true;
                var keeper = npc.AddComponent<NpcLeaver>();
                var cs = FindAnyObjectByType<CustomerSpawner>();
                if (cs != null && cs.entryPoint && cs.exitDoor && cs.exitPoint)
                    keeper.route = new[] { cs.entryPoint.position, cs.exitDoor.position, cs.exitPoint.position };   // marketin kapısından yola
                else keeper.route = new[] { npcExit };
            }
            Complete();
        }

        string StepText(int hay, int milk)
        {
            switch (step)
            {
                case Step.Move: return "Önce bir ısın bakalım: ekrana dokun, parmağını sürükle.";
                case Step.Hay: return "Aferin! Şimdi saman tarlasına git, balyaları topla.";
                case Step.Feed: return "Sarıkız acıkmıştır. Samanı yemliğine götür.";
                case Step.Milk: return "Bak süt veriyor! Şişeleri topla.";
                case Step.Shelf: return "Sütü markete götür, dolaba diz. Taze taze satılsın.";
                case Step.Checkout:
                    return checkout.QueueCount > 0 ? "Müşteri geldi! Kasaya geç, parasını al." : "Kasada bekle yeğenim, müşteri sütünü alıp gelecek.";
                case Step.Money: return "İlk kazancın hayırlı olsun! Kasanın yanındaki parayı topla.";
                case Step.Unlock:
                {
                    int need = cornZone.Remaining - GameManager.I.Money;
                    if (need <= 0) return "Para tamam! Mısır tarlasına git, parayı kilide dök.";
                    if (pile.amount > 0) loopTarget = pile;
                    else if (checkout.QueueCount > 0) loopTarget = checkout;
                    else if (milk > 0) loopTarget = milkShelf;
                    else if (cow.outputTray.Count > 0 || hay > 0 || cow.inputPile.Count > 0) loopTarget = cow;
                    else loopTarget = hayField;
                    return "Mısır tarlası için <color=#2A78CF>" + UIKit.Money(need) + " ₺</color> daha lazım. Biraz daha süt satalım.";
                }
                case Step.Tablet: return "Babanın tableti de artık sende. Sağ üstteki mavi tablete dokun.";
                case Step.Upgrade:
                {
                    var ui = OfficeUI.I;
                    string head = gifted ? "Al bakalım, benden harçlık! " : "";
                    if (!OfficeUI.IsOpen) return "Tableti yeniden aç yeğenim, yükseltmeyi yapalım.";
                    if (ui.HomeShown) return head + "Tabletten her şeyi geliştirirsin. <b>KARAKTER</b>'e dokun.";
                    if (ui.KarakterShown) return "Şimdi <b>KAPASİTE</b>'yi yükselt: daha çok ürün taşırsın.";
                    return "Geri dön, <b>KARAKTER</b>'e gir.";
                }
            }
            return "";
        }

        void SetText(string t)
        {
            if (t == fullText) return;
            fullText = t;
            typed = 0f;
        }

        void UpdateTyping()
        {
            typed = Mathf.Min(fullText.Length, typed + Time.deltaTime * 45f);
            int n = Mathf.FloorToInt(typed);
            // renk etiketinin ortasında kesme
            string shown = n >= fullText.Length ? fullText : SafeCut(fullText, n);
            if (boxText.text != shown) boxText.text = shown;
            bool talking = step == Step.Intro || step == Step.Outro;
            tapHint.gameObject.SetActive(talking && n >= fullText.Length);
            if (tapHint.gameObject.activeSelf) tapHint.anchoredPosition = new Vector2(-46f, 40f + Mathf.Abs(Mathf.Sin(Time.time * 4f)) * 8f);
        }

        static string SafeCut(string s, int n)
        {
            int open = s.LastIndexOf('<', Mathf.Max(0, n - 1));
            if (open >= 0 && open < n)
            {
                int close = s.IndexOf('>', open);
                if (close >= n) n = open;
            }
            var cut = s.Substring(0, n);
            if (cut.Contains("<color") && !cut.Contains("</color>")) cut += "</color>";
            return cut;
        }

        // ---------------- Hasan Amca ----------------

        void UpdateNpc()
        {
            if (npc == null) return;
            // konuşurken oyuncuya döner, uzaklaşınca yanına gelir
            var pp = player.transform.position;
            var np = npc.transform.position;
            var d = pp - np; d.y = 0f;
            bool talking = step == Step.Intro || step == Step.Outro;
            if (!talking && d.magnitude > 5.5f)
            {
                if (npcMover == null) npcMover = new NavMover(npc, 3.4f, 0.3f, 60);
                var side = pp - d.normalized * 2.2f;
                npcMover.MoveTo(side, 3.4f, 0.4f);
            }
            else if (d.sqrMagnitude > 0.01f)
            {
                if (npcMover != null) npcMover.MoveTo(np, 3.4f, 5f);   // dur
                npc.transform.rotation = Quaternion.Slerp(npc.transform.rotation, Quaternion.LookRotation(d), 1f - Mathf.Exp(-6f * Time.deltaTime));
            }
            waveTimer -= Time.deltaTime;
        }

        void LateUpdate()
        {
            // yeni cümlede el sallar
            if (!Active || npcArm == null || waveTimer <= 0f) return;
            float w = Mathf.Clamp01(waveTimer / 0.3f) * Mathf.Clamp01((1.4f - waveTimer) / 0.25f + 0.2f);
            var up = Quaternion.Euler(0f, 0f, -150f + Mathf.Sin(Time.time * 12f) * 18f);
            npcArm.localRotation = Quaternion.Slerp(npcArm.localRotation, up, w);
        }

        // ---------------- hedef ve görseller ----------------

        Station loopTarget;
        bool gifted;

        /// <summary>Tablet adımlarında parmağın göstereceği arayüz öğesi.</summary>
        RectTransform UiTarget()
        {
            if (step != Step.Tablet && step != Step.Upgrade) return null;
            var ui = OfficeUI.I;
            if (ui == null) return null;
            if (!OfficeUI.IsOpen) return OfficeUI.HudButton;
            if (step == Step.Tablet) return null;
            if (ui.HomeShown) return ui.KarakterBubble;
            if (ui.KarakterShown) return ui.BuyButtonFor(capUpgrade);
            if (ui.OtherListShown) return ui.BackButton;
            return null;
        }

        Station Target()
        {
            switch (step)
            {
                case Step.Hay: return hayField;
                case Step.Feed: case Step.Milk: return cow;
                case Step.Shelf: return milkShelf;
                case Step.Checkout: return checkout;
                case Step.Money: return pile;
                case Step.Unlock: return GameManager.I.Money >= cornZone.Remaining ? cornZone : loopTarget;
            }
            return null;
        }

        void UpdateVisuals()
        {
            bool tabletStep = step == Step.Tablet || step == Step.Upgrade;
            bool hidden = UIBlock.Active && !Blocking && !(tabletStep && OfficeUI.IsOpen);
            canvas.enabled = !hidden;
            bool talking = step == Step.Intro || step == Step.Outro;

            // tablet açıkken konuşma kutusu ekranın altına iner (tableti kapatmasın)
            bool low = tabletStep && OfficeUI.IsOpen;
            var anchor = low ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 1f);
            if (boxRt.anchorMin != anchor)
            {
                boxRt.anchorMin = boxRt.anchorMax = anchor;
                boxRt.pivot = anchor;
                boxRt.anchoredPosition = low ? new Vector2(16f, 8f) : new Vector2(20f, -250f);
                boxRt.localScale = Vector3.one * (low ? 0.8f : 1f);
            }
            tapCatcher.SetActive(talking);
            countPill.SetActive(!talking);
            if (!talking) countText.text = ((int)step) + "/" + Total;

            var tgt = talking ? null : Target();
            var cam = Camera.main;
            arrow.gameObject.SetActive(tgt != null && cam != null);
            pointerRt.gameObject.SetActive(false);

            // tablet adımları: parmak (yuvarlak ok) arayüzdeki hedefin altında zıplar
            var uiT = hidden ? null : UiTarget();
            if (uiT != null && uiT.gameObject.activeInHierarchy)
            {
                var sp = RectTransformUtility.WorldToScreenPoint(null, uiT.TransformPoint(uiT.rect.center));
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, sp, null, out var lp))
                {
                    float below = uiT.rect.height * uiT.lossyScale.y / ((RectTransform)canvas.transform).lossyScale.y * 0.5f + 70f;
                    pointerRt.gameObject.SetActive(true);
                    float bob = Mathf.Abs(Mathf.Sin(Time.time * 5f)) * 22f;
                    float half = uiT.rect.width * uiT.lossyScale.x / ((RectTransform)canvas.transform).lossyScale.x * 0.5f + 64f;
                    bool isBuy = capUpgrade != null && uiT == OfficeUI.I.BuyButtonFor(capUpgrade);
                    if (isBuy)
                    {
                        // satın al butonu: üstünden aşağı gösterir (soldaki açıklama yazısını kapatmasın)
                        pointerRt.anchoredPosition = lp + new Vector2(0f, below - 22f + bob);
                        pointerArrow.localEulerAngles = new Vector3(0, 0, 180f);
                        pointerRt.localScale = Vector3.one * 0.75f;
                    }
                    else if (uiT == OfficeUI.HudButton)
                    {
                        // sağdaki butonlar: soldan sağa gösterir (yazıların üstüne binmesin)
                        pointerRt.anchoredPosition = lp + new Vector2(-half - bob, 0f);
                        pointerArrow.localEulerAngles = new Vector3(0, 0, -90f);
                    }
                    else
                    {
                        // baloncuk / geri butonu: sağdan sola gösterir
                        pointerRt.anchoredPosition = lp + new Vector2(half + bob, 0f);
                        pointerArrow.localEulerAngles = new Vector3(0, 0, 90f);
                    }
                    if (!isBuy) pointerRt.localScale = Vector3.one * 0.9f;
                }
            }
            joyRt.gameObject.SetActive(step == Step.Move && !hidden);
            if (step == Step.Move)
            {
                float a = Time.time * 2.4f;
                joyKnob.anchoredPosition = new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.6f) * 60f;
            }
            if (tgt == null || cam == null) return;

            Vector3 basePos = tgt.transform.position + tgt.zoneOffset;
            float h = TopOf(tgt) + 0.5f;
            float bounce = Mathf.Abs(Mathf.Sin(Time.time * 4f)) * 0.35f;
            arrow.position = new Vector3(basePos.x, h + bounce, basePos.z);
            arrow.rotation = cam.transform.rotation;
            float pop = Mathf.Min(1f, stepAge * 5f);
            arrow.localScale = Vector3.one * Mathf.LerpUnclamped(0.2f, 1f, EaseOutBack(pop));
            if (hidden) return;

            var v = cam.WorldToViewportPoint(arrow.position);
            bool behind = v.z < 0f;
            if (behind) { v.x = 1f - v.x; v.y = 1f - v.y; }
            // üstte konuşma kutusu var: hedef kutunun altında kalıyorsa da kenar oku göster
            bool onScreen = !behind && v.x > 0.06f && v.x < 0.94f && v.y > 0.1f && v.y < 0.68f;
            if (onScreen) return;

            var rect = ((RectTransform)canvas.transform).rect;
            Vector2 d = new Vector2(v.x - 0.5f, v.y - 0.5f);
            if (d.sqrMagnitude < 1e-5f) d = Vector2.down;
            float hx = 0.5f - 0.08f, hyTop = 0.18f, hyBot = 0.5f - 0.12f;   // üst sınır konuşma kutusunun altı
            float sx = Mathf.Abs(d.x) > 1e-4f ? hx / Mathf.Abs(d.x) : float.MaxValue;
            float sy = Mathf.Abs(d.y) > 1e-4f ? (d.y > 0 ? hyTop : hyBot) / Mathf.Abs(d.y) : float.MaxValue;
            var e = d * Mathf.Min(sx, sy);
            pointerRt.gameObject.SetActive(true);
            pointerRt.anchoredPosition = new Vector2(e.x * rect.width, e.y * rect.height);
            float ang = Mathf.Atan2(d.y * rect.height, d.x * rect.width) * Mathf.Rad2Deg - 90f;
            pointerArrow.localEulerAngles = new Vector3(0, 0, ang);
            pointerRt.localScale = Vector3.one * (1f + 0.07f * Mathf.Abs(Mathf.Sin(Time.time * 5f)));
        }

        static float TopOf(Station s)
        {
            float top = 0.8f;
            foreach (var r in s.GetComponentsInChildren<MeshRenderer>())
            {
                if (!r.enabled || r.GetComponentInParent<WorldLabel>() != null) continue;
                top = Mathf.Max(top, r.bounds.max.y);
            }
            return Mathf.Min(top, 3.6f);
        }

        static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        void BuildArrow()
        {
            var root = new GameObject("TutorialArrow").transform;
            arrow = root;
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var yellow = new Material(lit); yellow.SetColor("_BaseColor", new Color(1f, 0.83f, 0.2f)); yellow.SetFloat("_Smoothness", 0.4f);
            var ink = new Material(lit); ink.SetColor("_BaseColor", new Color(0.17f, 0.18f, 0.26f)); ink.SetFloat("_Smoothness", 0f);
            yellow.SetFloat("_Cull", 0f); ink.SetFloat("_Cull", 0f);
            AddArrowMesh(root, 1.0f, 0.10f, 0f, yellow);
            AddArrowMesh(root, 1.22f, 0.06f, 0.08f, ink);
            arrow.gameObject.SetActive(false);
        }

        static void AddArrowMesh(Transform parent, float scale, float depth, float back, Material m)
        {
            Vector2[] p =
            {
                new Vector2(0f, 0f), new Vector2(0.5f, 0.55f), new Vector2(0.2f, 0.55f), new Vector2(0.2f, 1.15f),
                new Vector2(-0.2f, 1.15f), new Vector2(-0.2f, 0.55f), new Vector2(-0.5f, 0.55f)
            };
            var c = new Vector2(0f, 0.55f);
            for (int i = 0; i < p.Length; i++) p[i] = c + (p[i] - c) * scale;
            int n = p.Length;
            var verts = new List<Vector3>();
            var tris = new List<int>();
            float zf = -depth + back, zb = depth + back;
            int[][] faces = { new[] { 0, 1, 6 }, new[] { 2, 3, 4 }, new[] { 2, 4, 5 } };
            foreach (var f in faces)
            {
                int b0 = verts.Count;
                foreach (var k in f) verts.Add(new Vector3(p[k].x, p[k].y, zf));
                tris.Add(b0); tris.Add(b0 + 2); tris.Add(b0 + 1);
                int b1 = verts.Count;
                foreach (var k in f) verts.Add(new Vector3(p[k].x, p[k].y, zb));
                tris.Add(b1); tris.Add(b1 + 1); tris.Add(b1 + 2);
            }
            for (int i = 0; i < n; i++)
            {
                var a = p[i]; var b = p[(i + 1) % n];
                int s = verts.Count;
                verts.Add(new Vector3(a.x, a.y, zf)); verts.Add(new Vector3(b.x, b.y, zf));
                verts.Add(new Vector3(b.x, b.y, zb)); verts.Add(new Vector3(a.x, a.y, zb));
                tris.Add(s); tris.Add(s + 1); tris.Add(s + 2);
                tris.Add(s); tris.Add(s + 2); tris.Add(s + 3);
            }
            var mesh = new Mesh { name = "TutorialArrow" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var go = new GameObject("Arrow", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = m;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        void BuildUI()
        {
            canvas = UIKit.MakeCanvas("TutorialCanvas", transform, 35);
            var root = canvas.transform;

            // konuşma sırasında ekranın her yerine dokunmak "devam" demek
            var catcher = UIKit.Img("TapCatcher", root, null, new Color(0f, 0f, 0f, 0.001f));
            catcher.raycastTarget = true;
            UIKit.Stretch(catcher.rectTransform);
            catcher.gameObject.AddComponent<Button>().onClick.AddListener(OnTap);
            tapCatcher = catcher.gameObject;

            // konuşma kutusu (HUD'un altında)
            boxRt = UIKit.Node("Dialog", root);
            UIKit.Place(boxRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(20f, -250f), new Vector2(960f, 262f));

            var box = UIKit.MakeChunk("Box", boxRt, UIKit.Cream, UIKit.CreamLip, 52f, 6f, 12f);
            UIKit.Place(box.Rect, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(70f, 0f), new Vector2(890f, 232f));
            var face = box.Face.transform;
            boxText = UIKit.Label("Text", face, "", 35, UIKit.TextDark, TextAnchor.MiddleLeft, false, false);
            boxText.lineSpacing = 0.95f;
            // üstte isim etiketi ve GEÇ, sağ altta devam oku var: yazı aralarında kalsın
            UIKit.Stretch(boxText.rectTransform, 148f, 10f, 64f, 42f);

            // isim etiketi
            var tag = UIKit.MakeChunk("Name", boxRt, UIKit.Hex("5E7A4A"), UIKit.Hex("3E5530"), 30f, 5f, 7f, false);
            UIKit.Place(tag.Rect, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(200f, -32f), new Vector2(250f, 62f));
            UIKit.Stretch(UIKit.Label("T", tag.Face.transform, Npc, 30, Color.white, TextAnchor.MiddleCenter, true).rectTransform, 0, -3, 0, 0);

            // portre (kutudan taşan yuvarlak)
            var po = UIKit.Img("Portrait", boxRt, UIKit.Circle, UIKit.Ink);
            UIKit.Place(po.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 10f), new Vector2(200f, 200f));
            var pf = UIKit.Img("Bg", po.transform, UIKit.Circle, UIKit.Hex("BFEAFF"));
            UIKit.Stretch(pf.rectTransform, 8, 8, 8, 8);
            var mask = pf.gameObject.AddComponent<Mask>(); mask.showMaskGraphic = true;
            var face2 = UIKit.Img("Face", pf.transform, UIKit.Icon("hasan_amca"), Color.white);
            face2.preserveAspect = true;
            UIKit.Stretch(face2.rectTransform, -6, -14, -6, 2);

            // adım sayacı (portrenin altında)
            var cp = UIKit.MakeChunk("Count", boxRt, UIKit.Hex("FF8A3D"), UIKit.Hex("D25E17"), 30f, 5f, 7f, false);
            countPill = cp.Root.gameObject;
            UIKit.Place(cp.Rect, new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(100f, 6f), new Vector2(110f, 60f));
            countText = UIKit.Label("T", cp.Face.transform, "1/8", 30, Color.white, TextAnchor.MiddleCenter, true);
            UIKit.Stretch(countText.rectTransform, 0, -3, 0, 0);

            // GEÇ
            var skip = UIKit.MakeButton("Skip", boxRt, UIKit.Gray, UIKit.GrayLip, 32f, Skip, 5f, 8f);
            UIKit.Place(skip.Rect, new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-28f, -32f), new Vector2(112f, 64f));
            UIKit.Stretch(UIKit.Label("T", skip.Face.transform, "GEÇ", 28, Color.white, TextAnchor.MiddleCenter, true).rectTransform, 0, -3, 0, 0);
            UIKit.HudBlockers.Add(skip.Rect);

            // "devam" ipucu
            var th = UIKit.Img("TapHint", face, UIKit.PlayTri, UIKit.Hex("FF8A3D"));
            tapHint = th.rectTransform;
            UIKit.Place(tapHint, new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-46f, 40f), new Vector2(46f, 46f));
            tapHint.localEulerAngles = new Vector3(0, 0, -90f);

            // ekran dışı hedef için kenar oku
            pointerRt = UIKit.Node("Pointer", root);
            UIKit.Place(pointerRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(116f, 116f));
            UIKit.Stretch(UIKit.Img("Outline", pointerRt, UIKit.Circle, UIKit.Ink).rectTransform);
            UIKit.Stretch(UIKit.Img("Fill", pointerRt, UIKit.Circle, UIKit.Hex("FFD34D")).rectTransform, 7, 7, 7, 7);
            pointerArrow = UIKit.Img("Arrow", pointerRt, UIKit.ArrowUp, UIKit.Ink).rectTransform;
            UIKit.Stretch(pointerArrow, 22, 22, 22, 22);
            pointerRt.gameObject.SetActive(false);

            // joystick ipucu
            joyRt = UIKit.Node("JoyHint", root);
            UIKit.Place(joyRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 430f), new Vector2(240f, 240f));
            UIKit.Stretch(UIKit.Img("Base", joyRt, UIKit.Circle, new Color(1f, 1f, 1f, 0.35f)).rectTransform);
            joyKnob = UIKit.Img("Knob", joyRt, UIKit.Circle, UIKit.Ink).rectTransform;
            UIKit.Place(joyKnob, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(104f, 104f));
            UIKit.Stretch(UIKit.Img("Fill", joyKnob, UIKit.Circle, Color.white).rectTransform, 7, 7, 7, 7);
            joyRt.gameObject.SetActive(false);
        }
    }

    /// <summary>Rehber bitince Hasan Amca'yı yola yürütüp kaybeder (rehber nesnesi yok olduktan sonra da çalışır).</summary>
    public class NpcLeaver : MonoBehaviour
    {
        public Vector3[] route;
        NavMover mover;
        int leg;
        float t;

        void Update()
        {
            t += Time.deltaTime;
            if (t < 0.6f) return;   // önce bir el sallasın
            if (mover == null) mover = new NavMover(gameObject, 3.2f, 0.3f, 60);
            if (route == null || leg >= route.Length || t > 40f) { gameObject.SetActive(false); Destroy(this); return; }
            if (mover.MoveTo(route[leg], 3.2f, 0.6f)) leg++;
        }
    }
}
