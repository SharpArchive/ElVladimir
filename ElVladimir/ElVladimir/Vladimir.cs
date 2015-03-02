using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;


namespace ElVladimir
{
    /// <summary>
    ///     Handle all stuff what is going on with Vladimir.
    /// </summary>
    internal class Vladimir
    {
        private static String hero = "Vladimir";
        private static Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        private static Menu _menu;
        private static Orbwalking.Orbwalker _orbwalker;
        private static Spell Q, W, E, R;
        private static List<Spell> SpellList;

         // Summoner spells
        private static SpellSlot Ignite;

        public static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.BaseSkinName != hero)
                return;

            Console.WriteLine("ElVlad injected");
            Notifications.AddNotification("ElVladimir by jQuery v1.2 loaded", 5);

            #region Spell Data

            // set spells
            Q = new Spell(SpellSlot.Q, 600);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 610);
            R = new Spell(SpellSlot.R, 625);

            R.SetSkillshot(0.25f, 175, 700, false, SkillshotType.SkillshotCircle);
            SpellList = new List<Spell> { Q, E, W, R };

            // Ignite
            Ignite = Player.GetSpellSlot("summonerdot");

            InitializeMenu();

            #endregion

            Game.OnGameUpdate += OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
        }

        #region OnGameUpdate

        private static void OnGameUpdate(EventArgs args)
        {
            switch (_orbwalker.ActiveMode)
            {
               case Orbwalking.OrbwalkingMode.Combo:
                    Combo();
                break;

                case Orbwalking.OrbwalkingMode.Mixed:
                    Harass();
                break;

                case Orbwalking.OrbwalkingMode.LaneClear:
                    LaneClear();
                break;
            }

            if (Player.HasBuff("Recall") || Utility.InFountain(Player)) return;
            if (_menu.Item("EStack", true).GetValue<KeyBind>().Active)
            {
                if (Environment.TickCount - E.LastCastAttemptT >= 9900 && E.IsReady() &&
                (Player.Health / Player.MaxHealth) * 100 >= 
                _menu.Item("EStackHP").GetValue<Slider>().Value)
                    E.Cast();
            }

            if (_menu.Item("AutoHarass", true).GetValue<KeyBind>().Active)
            {
                var target = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Magical);
                if (target == null || !target.IsValid)
                {
                    return;
                }

                var qAutoHarass = _menu.Item("UseQAutoHarass").GetValue<bool>();
                var eAutoHarass = _menu.Item("UseEAutoHarass").GetValue<bool>();

                foreach (var spell in SpellList.Where(z => z.IsReady()))
                {
                    if (spell.Slot == SpellSlot.Q && qAutoHarass && Q.IsReady())
                    {
                        Q.Cast(target);
                    }

                    if (spell.Slot == SpellSlot.E && eAutoHarass && E.IsReady() && Player.Distance(target) <= E.Range 
                        && (Player.Health / Player.MaxHealth) * 100 >= _menu.Item("HarassHP").GetValue<Slider>().Value)
                    {
                        E.Cast(target);
                    }
                }
            }
        }

        #endregion


        #region GetComboDamage   

        private static float GetComboDamage(Obj_AI_Base enemy)
        {
            var damage = 0d;

            if (Q.IsReady())
            {
                damage += Player.GetSpellDamage(enemy, SpellSlot.Q);
            }

            if (W.IsReady())
            {
                damage += Player.GetSpellDamage(enemy, SpellSlot.W);
            }

            if (E.IsReady())
            {
                damage += Player.GetSpellDamage(enemy, SpellSlot.E);
            }

            if (R.IsReady())
            {
                damage += Player.GetSpellDamage(enemy, SpellSlot.R);
            }

            return (float)damage;
        }

        #endregion

        //new logic such OP 
        #region GetUltComboDamage   

        private static float GetUltComboDamage(Obj_AI_Base enemy)
        {
            var damage = 0d;

            if (R.IsReady())
            {
                damage += Player.GetSpellDamage(enemy, SpellSlot.R);
            }

            return (float)damage;
        }

        #endregion

        #region Combo

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Magical);
            if (target == null || !target.IsValid)
            {
                return;
            }

            var qCombo = _menu.Item("QCombo").GetValue<bool>();
            var wCombo = _menu.Item("WCombo").GetValue<bool>();
            var eCombo = _menu.Item("ECombo").GetValue<bool>();
            var rCombo = _menu.Item("RCombo").GetValue<bool>();
            var onlyKill = _menu.Item("RWhenKill").GetValue<bool>();
            var ultCount = _menu.Item("rcount").GetValue<Slider>().Value;
            var smartUlt = _menu.Item("SmartUlt").GetValue<bool>();


            var comboDamage = GetComboDamage(target);
            var getUltComboDamage = GetUltComboDamage(target);


            foreach (var spell in SpellList.Where(x => x.IsReady()))
            {

                if (spell.Slot == SpellSlot.Q && qCombo && Q.IsReady())
                {
                    Q.Cast(target);
                }
                
                //only kill with ult
                if (onlyKill && E.IsReady() && rCombo && Q.IsReady() && ObjectManager.Get<Obj_AI_Hero>().Count(hero => hero.IsValidTarget(R.Range)) >= ultCount)
                {
                    if (comboDamage >= target.Health)
                    {
                        R.CastOnUnit(target);
                    }
                }

                // When fighting and target can we killed with ult it will ult
                if (onlyKill && R.IsReady() && smartUlt)
                {
                    if (getUltComboDamage >= target.Health)
                    {
                        R.CastOnUnit(Player);
                    }
                }

                //not active
                if (!onlyKill && E.IsReady() && rCombo && Q.IsReady() && ObjectManager.Get<Obj_AI_Hero>().Count(hero => hero.IsValidTarget(R.Range)) >= ultCount)
                {
                    R.CastOnUnit(target);
                }

                if (spell.Slot == SpellSlot.E && eCombo && E.IsReady() && Player.Distance(target) <= E.Range)
                {
                    E.Cast(target);
                }

                if (spell.Slot == SpellSlot.W && wCombo && W.IsReady())
                {
                    W.CastOnUnit(Player);
                }
            }

            if (Player.Distance(target) <= 600 && IgniteDamage(target) >= target.Health &&
                _menu.Item("UseIgnite").GetValue<bool>())
            {
                Player.Spellbook.CastSpell(Ignite, target);
            }
        }

        #endregion

        #region Harass

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Magical);
            if (target == null || !target.IsValid)
            {
                return;
            }

            var qHarass = _menu.Item("HarassQ").GetValue<bool>();
            var eHarass = _menu.Item("HarassE").GetValue<bool>();

            foreach (var spell in SpellList.Where(y => y.IsReady()))
            {
                if (spell.Slot == SpellSlot.Q && qHarass && Q.IsReady())
                {
                    Q.Cast(target);
                }

                if (spell.Slot == SpellSlot.E && eHarass && E.IsReady() && Player.Distance(target) <= E.Range)
                {
                    E.Cast(target);
                }  
            }
        }

        #endregion

        #region Waveclear

        private static void LaneClear()
        {
            var minion = MinionManager.GetMinions(Player.ServerPosition, W.Range).FirstOrDefault();
            if (minion == null || minion.Name.ToLower().Contains("ward")) return;

            var qWaveClear = _menu.Item("WaveClearQ").GetValue<bool>();
            var eWaveClear = _menu.Item("WaveClearE").GetValue<bool>();

            if (qWaveClear && Q.IsReady())
            {
                Q.CastOnUnit(minion);
            }

            if (eWaveClear && E.IsReady() &&
                (Player.Health / Player.MaxHealth) * 100 >= _menu.Item("EStackHP").GetValue<Slider>().Value)
            {
                E.CastOnUnit(minion);
            }  
        }

        #endregion  

        #region Ignite

        private static float IgniteDamage(Obj_AI_Hero target)
        {
            if (Ignite == SpellSlot.Unknown || Player.Spellbook.CanUseSpell(Ignite) != SpellState.Ready)
            {
                return 0f;
            }
            return (float)Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);
        }

        #endregion

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            var AntiGapActive = _menu.Item("Antigap").GetValue<bool>();

            if (AntiGapActive && W.IsReady() && gapcloser.Sender.Distance(Player) < 300)
                W.Cast(Player);
        }

        #region Drawings

        private static void Drawing_OnDraw(EventArgs args)
        {
            var drawOff = _menu.Item("ElVladimir.Drawingsoff").GetValue<bool>();
            var drawQ = _menu.Item("ElVladimir.DrawQ").GetValue<Circle>();
            var drawW = _menu.Item("ElVladimir.DrawW").GetValue<Circle>();
            var drawE = _menu.Item("ElVladimir.DrawW").GetValue<Circle>();
            var drawR = _menu.Item("ElVladimir.DrawR").GetValue<Circle>();

            if (drawOff)
                return;

            if (drawQ.Active)
                if (Q.Level > 0)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range,Q.IsReady() ? Color.Green : Color.Red);

            if (drawW.Active)
                if (W.Level > 0)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, W.IsReady() ? Color.Green : Color.Red);

            if (drawE.Active)
                if (E.Level > 0)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range, E.IsReady() ? Color.Green : Color.Red);

            if (drawR.Active)
                if (R.Level > 0)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, R.Range, R.IsReady() ? Color.Green : Color.Red);
        }

        #endregion

        #region Menu

        private static void InitializeMenu()
        {
            _menu = new Menu("ElVladimir", hero, true);

            //Orbwalker
            var orbwalkerMenu = new Menu("Orbwalker", "orbwalker");
            _orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);
            _menu.AddSubMenu(orbwalkerMenu);

            //TargetSelector
            var targetSelector = new Menu("Target Selector", "TargetSelector");
            TargetSelector.AddToMenu(targetSelector);
            _menu.AddSubMenu(targetSelector);

            //Combo
            var comboMenu = _menu.AddSubMenu(new Menu("Combo", "Combo"));
            comboMenu.AddItem(new MenuItem("QCombo", "Use Q").SetValue(true));
            comboMenu.AddItem(new MenuItem("WCombo", "Use W").SetValue(false));
            comboMenu.AddItem(new MenuItem("ECombo", "Use E").SetValue(true));
            comboMenu.AddItem(new MenuItem("RCombo", "Use R").SetValue(true));
            comboMenu.AddItem(new MenuItem("fsfsafsaasffsa", ""));
            comboMenu.AddItem(new MenuItem("rcount", "Min target to R >= ")).SetValue(new Slider(1, 1, 5));
            comboMenu.AddItem(new MenuItem("RWhenKill", "Use R only when killable").SetValue(true));
            comboMenu.AddItem(new MenuItem("SmartUlt", "Use smart ult").SetValue(true));
            comboMenu.AddItem(new MenuItem("fsfsafsaasffs1111a", ""));
            comboMenu.AddItem(new MenuItem("UseIgnite", "Use Ignite in combo when killable").SetValue(true));
            comboMenu.AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));
           
            //Harass
            var harassMenu = _menu.AddSubMenu(new Menu("Harass", "H"));
            harassMenu.AddItem(new MenuItem("fsfsafsaasffsaxxxs", ""));
            harassMenu.AddItem(new MenuItem("HarassQ", "Use Q").SetValue(true));
            harassMenu.AddItem(new MenuItem("HarassE", "Use E").SetValue(true));

            //submenu harass
            harassMenu.SubMenu("AutoHarass").AddItem(new MenuItem("HarassHP", "[Auto harass] Minimum Health for E").SetValue(new Slider(20, 0, 100)));
            harassMenu.SubMenu("AutoHarass").AddItem(new MenuItem("AutoHarass", "[Toggle] Auto harass", true).SetValue(new KeyBind("L".ToCharArray()[0], KeyBindType.Toggle)));
            harassMenu.SubMenu("AutoHarass").AddItem(new MenuItem("spacespacespace", ""));
            harassMenu.SubMenu("AutoHarass").AddItem(new MenuItem("UseQAutoHarass", "Use Q").SetValue(true));
            harassMenu.SubMenu("AutoHarass").AddItem(new MenuItem("UseEAutoHarass", "Use E").SetValue(true));

            //Waveclear
            var waveClearMenu = _menu.AddSubMenu(new Menu("WaveClear", "waveclear"));
            waveClearMenu.AddItem(new MenuItem("fsfsafsaasffsadddd111", ""));
            waveClearMenu.AddItem(new MenuItem("WaveClearQ", "Use Q").SetValue(true));
            waveClearMenu.AddItem(new MenuItem("WaveClearE", "Use E").SetValue(true));
            waveClearMenu.SubMenu("LaneClearHealth").AddItem(new MenuItem("LaneClearHealth", "[WaveClear] Minimum Health for E").SetValue(new Slider(20, 0, 100)));
            waveClearMenu.AddItem(new MenuItem("WaveClearActive", "WaveClear!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));

            //Misc
            var miscMenu = _menu.AddSubMenu(new Menu("Drawings", "Misc"));
            miscMenu.AddItem(new MenuItem("ElVladimir.Drawingsoff", "Drawings off").SetValue(false));
            miscMenu.AddItem(new MenuItem("ElVladimir.DrawQ", "Draw Q").SetValue(new Circle()));
            miscMenu.AddItem(new MenuItem("ElVladimir.DrawW", "Draw W").SetValue(new Circle()));
            miscMenu.AddItem(new MenuItem("ElVladimir.DrawE", "Draw E").SetValue(new Circle()));
            miscMenu.AddItem(new MenuItem("ElVladimir.DrawR", "Draw R").SetValue(new Circle()));

            // Settings
            var settingsMenu = _menu.AddSubMenu(new Menu("Settings", "Settings"));
            settingsMenu.AddItem(new MenuItem("EStack", "[Toggle] Stack E", true).SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Toggle)));
            settingsMenu.AddItem(new MenuItem("fsfsafsaasffsaxx", ""));
            settingsMenu.AddItem(new MenuItem("EStackHP", "Don't stack when HP below % ")).SetValue(new Slider(20, 1, 100));
            settingsMenu.AddItem(new MenuItem("fsfsafsaasffsaxxxxx", ""));
            settingsMenu.AddItem(new MenuItem("Antigap", "Use W for gapclosers").SetValue(true));

            //Here comes the moneyyy, money, money, moneyyyy
            var credits = _menu.AddSubMenu(new Menu("Credits", "jQuery"));
            credits.AddItem(new MenuItem("ElKennen.Paypal", "if you would like to donate via paypal:"));
            credits.AddItem(new MenuItem("Elkennen.Email", "info@zavox.nl"));

            _menu.AddItem(new MenuItem("422442fsaafs4242f", ""));
            _menu.AddItem(new MenuItem("422442fsaafsf", "Version: 1.4"));
            _menu.AddItem(new MenuItem("fsasfafsfsafsa", "Made By jQuery"));

            _menu.AddToMainMenu();
        }
        #endregion
    }
}
