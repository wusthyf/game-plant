using PlantSpirit.GGJ;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PlantSpirit.GGJ.Editor
{
    public static class GGJContentFactory
    {
        private const string Data = "Assets/Game/Data";
        [MenuItem("Plant Spirit/Build Formal Demo Scenes")]
        public static void Build()
        {
            AudioAssetSetup.Build(); CreateContent(); GGJSceneGenerator.CreateScenes(); BuildMenu(); BuildLevel();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Game/Scenes/MainMenu.unity", true), new EditorBuildSettingsScene("Assets/Game/Scenes/Level01.unity", true) };
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        }
        [MenuItem("Plant Spirit/Rebuild Main Menu")]
        public static void BuildMainMenu()
        {
            AudioAssetSetup.Build(); GGJSceneGenerator.CreateScenes(); BuildMenu(); AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        }
        [MenuItem("Plant Spirit/Upgrade PC UI Layout")]
        public static void UpgradePcUiLayout()
        {
            string[] scenePaths = { "Assets/Game/Scenes/MainMenu.unity", "Assets/Game/Scenes/Level01.unity" };
            foreach (string scenePath in scenePaths)
            {
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                foreach (CanvasScaler scaler in Object.FindObjectsOfType<CanvasScaler>(true))
                {
                    ConfigureCanvasScaler(scaler);
                    EditorUtility.SetDirty(scaler);
                }
                if (scene.name == "Level01")
                {
                    GameUiController ui = Object.FindObjectOfType<GameUiController>(true);
                    SerializedObject uiData = new SerializedObject(ui);
                    Text hud = (Text)uiData.FindProperty("hud").objectReferenceValue;
                    Text interaction = (Text)uiData.FindProperty("interactionText").objectReferenceValue;
                    ConfigureHudRect(hud.rectTransform);
                    ConfigureInteractionRect(interaction.rectTransform);
                    EditorUtility.SetDirty(hud.rectTransform);
                    EditorUtility.SetDirty(interaction.rectTransform);
                }
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        [MenuItem("Plant Spirit/Create GGJ48H Content")]
        public static void CreateContent()
        {
            if (!AssetDatabase.IsValidFolder(Data)) AssetDatabase.CreateFolder("Assets/Game", "Data");
            PlayerConfig p = Asset<PlayerConfig>("PlayerConfig"); p.MaxHealth=100; p.MoveSpeed=5.2f; p.GroundAcceleration=45f; p.GroundDeceleration=55f; p.AirAcceleration=42f; p.JumpVelocity=12.5f; p.MaxFallSpeed=18f; p.CoyoteSeconds=.12f; p.JumpBufferSeconds=.12f; p.DashDistance=3.6f; p.DashSeconds=.22f; p.DashCooldown=1.1f; p.DashInvincibleSeconds=.18f; p.HurtInvincibleSeconds=.75f;
            Graft("vine_tendril",GraftSlot.Stem,"藤蔓触须","普攻变为藤鞭：8 伤害、3.4 范围，最多命中两个目标。",8,3.4f,.7f,0,false);
            Graft("toxic_cap",GraftSlot.Flower,"毒菌伞","技能变为毒雾：持续 3 秒，每秒 6 伤害并减速 30%。",6,3,5,0,false);
            Graft("iron_root",GraftSlot.Root,"铁甲根","受到伤害降低 25%，冲刺前段可挡普通投射物。",0,0,0,.25f,true);
            Attack("default_attack",AttackExecutorType.MeleeBox,10,.12f,.04f,.14f,.42f,new Vector2(1.05f,.1f),new Vector2(1.7f,.85f),1.7f,1);
            Attack("vine_attack",AttackExecutorType.VineLine,8,.14f,.06f,.14f,.7f,new Vector2(2f,.1f),new Vector2(3.4f,.85f),3.4f,2);
            Attack("seed_skill",AttackExecutorType.Projectile,8,.08f,0,.1f,2.5f,Vector2.zero,Vector2.zero,.5f,1);
            Attack("poison_skill",AttackExecutorType.PoisonZone,6,.1f,0,.15f,5,Vector2.zero,Vector2.zero,2,1); AssetDatabase.SaveAssets();
        }
        private static void BuildMenu()
        {
            Scene s=EditorSceneManager.OpenScene("Assets/Game/Scenes/MainMenu.unity",OpenSceneMode.Single); Clear(s); new GameObject("GameBootstrap").AddComponent<GameBootstrap>(); Camera(); EventSystem(); Canvas c=Canvas("MenuCanvas"); Label(c.transform,"植物精灵",new Vector2(0,210),54); Button start=Button(c.transform,"开始游戏",new Vector2(0,100)); Button controls=Button(c.transform,"操作说明",new Vector2(0,40)); Button audio=Button(c.transform,"音频设置",new Vector2(0,-20)); Button quit=Button(c.transform,"退出游戏",new Vector2(0,-80)); GameObject panel=Panel(c.transform,"A/D 移动   Space 跳跃   Shift 冲刺\n左键/J 普攻   右键/K 技能\nTab/G 嫁接   Esc 暂停   E 进入传送门",new Color(.08f,.18f,.12f,.96f)); panel.SetActive(false); GameObject audioPanel=Panel(c.transform,"",new Color(.08f,.18f,.12f,1f)); audioPanel.name="AudioSettingsPanel"; audioPanel.GetComponent<RectTransform>().sizeDelta=new Vector2(720,520); Label(audioPanel.transform,"音频设置",new Vector2(0,180),30); Slider master=SliderRow(audioPanel.transform,"主音量",105); Slider music=SliderRow(audioPanel.transform,"音乐",25); Slider effects=SliderRow(audioPanel.transform,"音效",-55); Button closeAudio=Button(audioPanel.transform,"关闭",new Vector2(0,-180)); AudioSettingsPanel settings=c.gameObject.AddComponent<AudioSettingsPanel>(); settings.Configure(audioPanel,closeAudio,master,music,effects); audioPanel.SetActive(false); c.gameObject.AddComponent<MainMenuPresenter>().Configure(start,controls,audio,quit,panel,settings); EditorSceneManager.SaveScene(s);
        }
        private static void BuildLevel()
        {
            Scene s=EditorSceneManager.OpenScene("Assets/Game/Scenes/Level01.unity",OpenSceneMode.Single); Clear(s); new GameObject("GameBootstrap").AddComponent<GameBootstrap>(); Camera(); EventSystem(); Ground(new Vector2(6,-4),new Vector2(66,.6f),"Ground"); Ground(new Vector2(-8,-2.6f),new Vector2(3.3f,.35f),"TutorialPlatform"); Ground(new Vector2(1,-2.0f),new Vector2(3.5f,.35f),"CombatPlatform"); Ground(new Vector2(12,-2.5f),new Vector2(3.5f,.35f),"CombatPlatform02"); Ground(new Vector2(23,-1.9f),new Vector2(3.5f,.35f),"CombatPlatform03");
            InputReader input=new GameObject("InputReader").AddComponent<InputReader>(); PlayerMotor2D motor=Player(Asset<PlayerConfig>("PlayerConfig"),out PlayerHealth health,out GraftInventory inv,out GraftApplier applier);
            KillPlane(new Vector2(6,-8.5f),new Vector2(70,.3f),motor);
            ExitPortal portal=Portal(new Vector2(29,-2.9f)); portal.gameObject.SetActive(false); EncounterZone[] zones={ Zone("Encounter01",0,new Vector2(-2,-2.2f),new[]{EnemyKind.Vine,EnemyKind.Vine},GraftAsset("vine_tendril"),motor.transform,inv), Zone("Encounter02",1,new Vector2(9,-2.2f),new[]{EnemyKind.Mushroom,EnemyKind.Vine},GraftAsset("toxic_cap"),motor.transform,inv), Zone("Encounter03",2,new Vector2(20,-2.2f),new[]{EnemyKind.Beetle,EnemyKind.Mushroom},GraftAsset("iron_root"),motor.transform,inv)}; GameObject gate01=Gate(new Vector2(3.5f,-2.3f),"Gate01"); GameObject gate02=Gate(new Vector2(14.5f,-2.3f),"Gate02"); Set(zones[0],"rightGate",gate01); Set(zones[1],"leftGate",gate01); Set(zones[1],"rightGate",gate02); Set(zones[2],"leftGate",gate02);
            LevelFlow flow=new GameObject("LevelFlow").AddComponent<LevelFlow>(); Set(flow,"encounters",zones); Set(flow,"portal",portal); UnityEngine.Camera.main.gameObject.AddComponent<CameraFollow2D>().Configure(motor.transform,-10f,25f); Ui(input,health,applier,portal); EditorSceneManager.SaveScene(s);
        }
        private static PlayerMotor2D Player(PlayerConfig cfg,out PlayerHealth health,out GraftInventory inv,out GraftApplier applier)
        {
            GameObject p=new GameObject("Player"); p.layer=9; p.transform.position=new Vector2(-13,-3.1f); p.AddComponent<PlaceholderVisual>().Configure(new Color(.62f,.95f,.35f),new Vector2(.55f,.9f),4); Rigidbody2D rb=p.AddComponent<Rigidbody2D>(); rb.gravityScale=3.2f; rb.freezeRotation=true; BoxCollider2D bc=p.AddComponent<BoxCollider2D>(); bc.size=Vector2.one; GameObject probe=new GameObject("GroundProbe"); probe.transform.SetParent(p.transform); probe.transform.localPosition=new Vector3(0,-.5f,0); PlayerMotor2D motor=p.AddComponent<PlayerMotor2D>(); motor.Configure(cfg,1<<8,probe.transform); health=p.AddComponent<PlayerHealth>(); health.Configure(cfg,motor); Hurtbox2D hb=p.AddComponent<Hurtbox2D>(); hb.Receiver=health; Hitbox2D hit=p.AddComponent<Hitbox2D>(); hit.Configure(1<<12); PlayerCombat combat=p.AddComponent<PlayerCombat>(); combat.Configure(motor,hit,AttackAsset("default_attack"),AttackAsset("seed_skill"),AttackAsset("vine_attack"),AttackAsset("poison_skill")); inv=p.AddComponent<GraftInventory>(); applier=p.AddComponent<GraftApplier>(); Set(applier,"combat",combat); return motor;
        }
        private static EncounterZone Zone(string name,int id,Vector2 center,EnemyKind[] kinds,GraftDefinition reward,Transform player,GraftInventory inv)
        { GameObject go=new GameObject(name); go.transform.position=center; BoxCollider2D box=go.AddComponent<BoxCollider2D>(); box.isTrigger=true; box.size=new Vector2(7.5f,4.2f); Transform[] spots=new Transform[kinds.Length]; for(int i=0;i<spots.Length;i++){GameObject spot=new GameObject(name+"Spawn"+i);spot.transform.position=center+new Vector2(1+i*1.3f,-.7f);spots[i]=spot.transform;} EncounterZone zone=go.AddComponent<EncounterZone>(); zone.Configure(id,player,inv,reward,kinds,spots);return zone; }
        private static ExitPortal Portal(Vector2 pos){GameObject go=new GameObject("ExitPortal");go.layer=14;go.transform.position=pos;go.AddComponent<PlaceholderVisual>().Configure(new Color(.2f,.95f,.65f),new Vector2(1.1f,1.6f),2);BoxCollider2D c=go.AddComponent<BoxCollider2D>();c.isTrigger=true;c.size=Vector2.one;return go.AddComponent<ExitPortal>();}
        private static GameObject Gate(Vector2 pos,string name){GameObject go=new GameObject(name);go.layer=8;go.transform.position=pos;go.AddComponent<PlaceholderVisual>().Configure(new Color(.55f,.18f,.14f),new Vector2(.35f,3.2f),3);BoxCollider2D c=go.AddComponent<BoxCollider2D>();c.size=Vector2.one;return go;}
        private static void Ui(InputReader input, PlayerHealth health, GraftApplier applier, ExitPortal portal)
        {
            Canvas canvas = Canvas("GameCanvas");
            Text hud = Label(canvas.transform, "", new Vector2(-360, 245), 22);
            hud.alignment = TextAnchor.MiddleLeft;
            ConfigureHudRect(hud.rectTransform);
            Text interaction = Label(canvas.transform, "按 E 进入传送门", new Vector2(0, -250), 24);
            ConfigureInteractionRect(interaction.rectTransform);
            interaction.gameObject.SetActive(false);

            GameObject graft = Panel(canvas.transform, "", new Color(.1f, .25f, .18f, .92f));
            GameObject pause = Panel(canvas.transform, "已暂停", new Color(.1f, .1f, .15f, .92f));
            GameObject dead = Panel(canvas.transform, "", new Color(.25f, .08f, .08f, .92f));
            GameObject result = Panel(canvas.transform, "", new Color(.08f, .25f, .15f, .92f));
            graft.SetActive(false); pause.SetActive(false); dead.SetActive(false); result.SetActive(false);

            Button root = Button(graft.transform, "根", new Vector2(-190, -72));
            Button stem = Button(graft.transform, "茎", new Vector2(0, -72));
            Button flower = Button(graft.transform, "花", new Vector2(190, -72));
            Button close = Button(graft.transform, "关闭", new Vector2(0, -142));
            GameUiController ui = canvas.gameObject.AddComponent<GameUiController>();
            ui.Configure(graft, pause, dead, result, hud, graft.GetComponentInChildren<Text>(), dead.GetComponentInChildren<Text>(), result.GetComponentInChildren<Text>(), interaction, health, applier, input, portal);
            Button resume = Button(pause.transform, "继续", new Vector2(-130, -68));
            Button pauseMenu = Button(pause.transform, "主菜单", new Vector2(130, -68));
            Button deadRestart = Button(dead.transform, "重新开始", new Vector2(-130, -88));
            Button deadMenu = Button(dead.transform, "主菜单", new Vector2(130, -88));
            Button retry = Button(result.transform, "再次挑战", new Vector2(-130, -120));
            Button resultMenu = Button(result.transform, "主菜单", new Vector2(130, -120));
            ui.ConfigureButtons(root, stem, flower, close, resume, pauseMenu, deadRestart, deadMenu, retry, resultMenu);
        }
        private static Canvas Canvas(string n){GameObject go=new GameObject(n);Canvas c=go.AddComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceOverlay;CanvasScaler sc=go.AddComponent<CanvasScaler>();ConfigureCanvasScaler(sc);go.AddComponent<GraphicRaycaster>();return c;}
        private static GameObject Panel(Transform p,string text,Color color){GameObject go=new GameObject(text.Length==0?"Panel":text);go.transform.SetParent(p,false);Image i=go.AddComponent<Image>();i.color=color;RectTransform r=i.rectTransform;r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.sizeDelta=new Vector2(720,420);Text label=Label(go.transform,text,new Vector2(0,55),26);label.rectTransform.sizeDelta=new Vector2(650,230);return go;}
        private static Text Label(Transform p,string text,Vector2 pos,int size){GameObject go=new GameObject(text.Length==0?"Label":text);go.transform.SetParent(p,false);Text t=go.AddComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.text=text;t.fontSize=size;t.color=Color.white;t.alignment=TextAnchor.MiddleCenter;RectTransform r=t.rectTransform;r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=new Vector2(1300,100);return t;}
        private static Button Button(Transform p,string text,Vector2 pos){GameObject go=new GameObject(text);go.transform.SetParent(p,false);Image i=go.AddComponent<Image>();i.color=new Color(.2f,.55f,.28f);Button b=go.AddComponent<Button>();go.AddComponent<AudioButtonFeedback>();RectTransform r=i.rectTransform;r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=new Vector2(150,48);Label(go.transform,text,Vector2.zero,20);return b;}
        private static Slider SliderRow(Transform p,string text,float y){Text label=Label(p,text,new Vector2(-175,y),21);label.rectTransform.sizeDelta=new Vector2(180,45);GameObject go=DefaultControls.CreateSlider(new DefaultControls.Resources());go.name=text;go.transform.SetParent(p,false);RectTransform r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(75,y);r.sizeDelta=new Vector2(330,32);Slider slider=go.GetComponent<Slider>();slider.minValue=0;slider.maxValue=1;slider.wholeNumbers=false;return slider;}
        private static void ConfigureCanvasScaler(CanvasScaler scaler){scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;scaler.matchWidthOrHeight=.5f;}
        private static void ConfigureHudRect(RectTransform rect){rect.anchorMin=rect.anchorMax=new Vector2(0f,1f);rect.pivot=new Vector2(0f,1f);rect.anchoredPosition=new Vector2(24f,-24f);rect.sizeDelta=new Vector2(900f,76f);}
        private static void ConfigureInteractionRect(RectTransform rect){rect.anchorMin=rect.anchorMax=new Vector2(.5f,0f);rect.pivot=new Vector2(.5f,0f);rect.anchoredPosition=new Vector2(0f,24f);rect.sizeDelta=new Vector2(900f,60f);}
        private static void Ground(Vector2 pos,Vector2 size,string name){GameObject g=new GameObject(name);g.layer=8;g.transform.position=pos;g.AddComponent<PlaceholderVisual>().Configure(new Color(.20f,.38f,.22f),size,1);BoxCollider2D c=g.AddComponent<BoxCollider2D>();c.size=Vector2.one;} private static void KillPlane(Vector2 pos,Vector2 size,PlayerMotor2D motor){GameObject g=new GameObject("KillPlane");g.transform.position=pos;BoxCollider2D c=g.AddComponent<BoxCollider2D>();c.isTrigger=true;c.size=size;g.AddComponent<KillPlane>().Configure(motor.transform.position,motor);} private static void Camera(){GameObject go=new GameObject("Main Camera");go.tag="MainCamera";Camera c=go.AddComponent<Camera>();c.orthographic=true;c.orthographicSize=5.4f;c.backgroundColor=new Color(.07f,.12f,.1f);go.transform.position=new Vector3(5,0,-10);} private static void EventSystem(){GameObject go=new GameObject("EventSystem");go.AddComponent<EventSystem>();go.AddComponent<StandaloneInputModule>();} private static void Clear(Scene s){foreach(GameObject o in s.GetRootGameObjects())Object.DestroyImmediate(o);} private static T Asset<T>(string name)where T:ScriptableObject{string path=Data+"/"+name+".asset";T a=AssetDatabase.LoadAssetAtPath<T>(path);if(a!=null)return a;a=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(a,path);return a;} private static GraftDefinition GraftAsset(string id)=>AssetDatabase.LoadAssetAtPath<GraftDefinition>(Data+"/Graft_"+id+".asset"); private static AttackDefinition AttackAsset(string id)=>AssetDatabase.LoadAssetAtPath<AttackDefinition>(Data+"/Attack_"+id+".asset");
        private static void Graft(string id,GraftSlot slot,string name,string desc,float dmg,float range,float cd,float reduction,bool shield){GraftDefinition g=Asset<GraftDefinition>("Graft_"+id);g.Id=id;g.Slot=slot;g.DisplayName=name;g.Description=desc;g.AttackDamage=dmg;g.AttackRange=range;g.Cooldown=cd;g.DamageReduction=reduction;g.BlocksProjectilesDuringDash=shield;EditorUtility.SetDirty(g);} private static void Attack(string id,AttackExecutorType type,float damage,float startup,float active,float recovery,float cooldown,Vector2 offset,Vector2 size,float range,int targets){AttackDefinition a=Asset<AttackDefinition>("Attack_"+id);a.Id=id;a.Executor=type;a.Damage=damage;a.Startup=startup;a.Active=active;a.Recovery=recovery;a.Cooldown=cooldown;a.Offset=offset;a.Size=size;a.Range=range;a.MaxTargets=targets;EditorUtility.SetDirty(a);} private static void Set(object obj,string field,object value){obj.GetType().GetField(field,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)?.SetValue(obj,value);}
    }
}
