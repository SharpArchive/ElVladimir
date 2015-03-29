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
    internal enum Spells
    {
        Q,
        W,
        E,
        R
    }

    internal class Vladimir
    {
        private static Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        private static Menu _menu;
        private static Orbwalking.Orbwalker _orbwalker;
        private static List<Spell> SpellList;

         // Summoner spells
        private static SpellSlot Ignite;
        public static Dictionary<Spells, Spell> spells = new Dictionary<Spells, Spell>()
        {
            { Spells.Q, new Spell(SpellSlot.Q, 600)},
            { Spells.W, new Spell(SpellSlot.W)},
            { Spells.E, new Spell(SpellSlot.E, 610)},
            { Spells.R, new Spell(SpellSlot.R, 625)}
        };

        public static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.BaseSkinName != "Vladimir")
                return;

            Notifications.AddNotification("ElVladimir by jQuery 3.0.0.0", 5000);

            #region Spell Data

            spells[Spells.R].SetSkillshot(0.25f, 175, 700, false, SkillshotType.SkillshotCircle);
            Ignite = Player.GetSpellSlot("summonerdot");

            InitializeMenu();

            #endregion

            Console.WriteLine("Injected");

            Game.OnUpdate += OnGameUpdate;
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

            if (_menu.Item("ElVladimir.Lasthit").GetValue<KeyBind>().Active)
            {
                var allMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, spells[Spells.Q].Range);
                {
                    foreach (
                        var minion in
                            allMinions.Where(
                                minion => minion.Health <= ObjectManager.Player.GetSpellDamage(minion, SpellSlot.Q)))
                    {
                        if (minion.IsValidTarget())
                        {
                            spells[Spells.Q].CastOnUnit(minion);
                            return;
                        }
                    }
                }
            }

            if (Player.IsRecalling() || Player.InFountain()) return;
            if (_menu.Item("EStack", true).GetValue<KeyBind>().Active)
            {
                if (Environment.TickCount - spells[Spells.E].LastCastAttemptT >= 9900 && spells[Spells.E].IsReady() &&
                (Player.Health / Player.MaxHealth) * 100 >= 
                _menu.Item("EStackHP").GetValue<Slider>().Value)
                    spells[Spells.E].Cast();
            }

            var target = TargetSelector.GetTarget(spells[Spells.Q].Range, TargetSelector.DamageType.Magical);

            if (_menu.Item("AutoHarass", true).GetValue<KeyBind>().Active)
            {
                if (target == null || !target.IsValid)
                    return;

                var qAutoHarass = _menu.Item("UseQAutoHarass").GetValue<bool>();
                var eAutoHarass = _menu.Item("UseEAutoHarass").GetValue<bool>();

                if (qAutoHarass && spells[Spells.Q].IsReady())
                {
                    spells[Spells.Q].Cast(target);
                }

                if (eAutoHarass && spells[Spells.E].IsReady() && Player.Distance(target) <= spells[Spells.E].Range 
                    && (Player.Health / Player.MaxHealth) * 100 >= _menu.Item("HarassHP").GetValue<Slider>().Value)
                {
                    spells[Spells.E].Cast(target);
                }
            }
        }

        #endregion

        #region GetComboDamage   

        private static float GetComboDamage(Obj_AI_Base enemy)
        {
            var damage = 0d;

            if (spells[Spells.Q].IsReady())
            {
                damage += Player.GetSpellDamage(enemy, SpellSlot.Q);
            }

            if (spells[Spells.W].IsReady())
            {
                damage += Player.GetSpellDamage(enemy, SpellSlot.W);
            }

            if (spells[Spells.E].IsReady())
            {
                damage += Player.GetSpellDamage(enemy, SpellSlot.E);
            }

            if (spells[Spells.R].IsReady())
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

            if (spells[Spells.R].IsReady())
            {
                damage += Player.GetSpellDamage(enemy, SpellSlot.R);
            }

            return (float)damage;
        }

        #endregion

        #region Combo

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(spells[Spells.Q].Range, TargetSelector.DamageType.Magical);
            if (target == null || !target.IsValid)
                return;

            var useQ = _menu.Item("QCombo").GetValue<bool>();
            var useW = _menu.Item("WCombo").GetValue<bool>();
            var useE = _menu.Item("ECombo").GetValue<bool>();
            var rCombo = _menu.Item("RCombo").GetValue<bool>();
            var onlyKill = _menu.Item("RWhenKill").GetValue<bool>();
            var ultCount = _menu.Item("rcount").GetValue<Slider>().Value;
            var smartUlt = _menu.Item("SmartUlt").GetValue<bool>();
            var comboDamage = GetComboDamage(target);
            var getUltComboDamage = GetUltComboDamage(target);

            if (useQ && target.IsValidTarget())
            {
                spells[Spells.Q].CastOnUnit(target, true);
            }
                
            if (onlyKill && spells[Spells.E].IsReady() && rCombo && ObjectManager.Get<Obj_AI_Hero>().Count(hero => hero.IsValidTarget(spells[Spells.R].Range)) >= ultCount)
            {
                if (comboDamage >= target.Health)
                {
                    spells[Spells.R].Cast(target);
                }
            }

            if (onlyKill && smartUlt)
            {
                if (getUltComboDamage >= target.Health)
                {
                    spells[Spells.R].Cast(target);
                }
            }

            if (!onlyKill && spells[Spells.E].IsReady() && rCombo &&  ObjectManager.Get<Obj_AI_Hero>().Count(hero => hero.IsValidTarget(spells[Spells.R].Range)) >= ultCount)
            {
                spells[Spells.R].Cast(target);
            }

            if (useE && spells[Spells.E].IsReady() && Player.Distance(target) <= spells[Spells.E].Range)
            {
                spells[Spells.E].Cast(target);
            }

            if (useW && spells[Spells.W].IsReady())
            {
                spells[Spells.W].Cast(Player);
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
            var target = TargetSelector.GetTarget(spells[Spells.Q].Range, TargetSelector.DamageType.Magical);

            if (target == null || !target.IsValid)
                return;

            var qHarass = _menu.Item("HarassQ").GetValue<bool>();
            var eHarass = _menu.Item("HarassE").GetValue<bool>();

            if (qHarass && spells[Spells.Q].IsReady())
            {
                spells[Spells.Q].Cast(target);
            }

            if (eHarass && spells[Spells.E].IsReady() && Player.Distance(target.ServerPosition) <= spells[Spells.E].Range)
            {
                spells[Spells.E].Cast(target);
            }  
        }

        #endregion

        #region Waveclear

        private static void LaneClear()
        {
            var minion = MinionManager.GetMinions(Player.ServerPosition, spells[Spells.W].Range).FirstOrDefault();
            if (minion == null || minion.Name.ToLower().Contains("ward")) return;

            var qWaveClear = _menu.Item("WaveClearQ").GetValue<bool>();
            var eWaveClear = _menu.Item("WaveClearE").GetValue<bool>();

            if (qWaveClear && spells[Spells.Q].IsReady())
            {
                spells[Spells.Q].CastOnUnit(minion);
            }

            if (eWaveClear && spells[Spells.E].IsReady() &&
                (Player.Health / Player.MaxHealth) * 100 >= _menu.Item("EStackHP").GetValue<Slider>().Value)
            {
                spells[Spells.E].CastOnUnit(minion);
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

            if (AntiGapActive && spells[Spells.W].IsReady() && gapcloser.Sender.Distance(Player) < 300)
                spells[Spells.W].Cast(Player);
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
                if (spells[Spells.Q].Level > 0)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, spells[Spells.Q].Range, spells[Spells.Q].IsReady() ? Color.Green : Color.Red);

            if (drawW.Active)
                if (spells[Spells.W].Level > 0)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, spells[Spells.W].Range, spells[Spells.W].IsReady() ? Color.Green : Color.Red);

            if (drawE.Active)
                if (spells[Spells.E].Level > 0)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, spells[Spells.E].Range, spells[Spells.E].IsReady() ? Color.Green : Color.Red);

            if (drawR.Active)
                if (spells[Spells.R].Level > 0)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, spells[Spells.R].Range, spells[Spells.R].IsReady() ? Color.Green : Color.Red);
        }

        #endregion

        #region Menu

        private static void InitializeMenu()
        {
            _menu = new Menu("ElVladimir", "Vladimir", true);

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
            var miscMenu = _menu.AddSubMenu(new Menu("Misc", "Misc"));
            miscMenu.AddItem(new MenuItem("ElVladimir.Drawingsoff", "Drawings off").SetValue(false));
            miscMenu.AddItem(new MenuItem("ElVladimir.DrawQ", "Draw Q").SetValue(new Circle()));
            miscMenu.AddItem(new MenuItem("ElVladimir.DrawW", "Draw W").SetValue(new Circle()));
            miscMenu.AddItem(new MenuItem("ElVladimir.DrawE", "Draw E").SetValue(new Circle()));
            miscMenu.AddItem(new MenuItem("ElVladimir.DrawR", "Draw R").SetValue(new Circle()));
            miscMenu.AddItem(new MenuItem("asffasasfafs", ""));
            miscMenu.AddItem(new MenuItem("ElVladimir.Lasthit", "Last Hit Key").SetValue(new KeyBind("Z".ToCharArray()[0], KeyBindType.Press)));


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
            _menu.AddItem(new MenuItem("422442fsaafsf", "Version: 2.4"));
            _menu.AddItem(new MenuItem("fsasfafsfsafsa", "Made By jQuery"));

            _menu.AddToMainMenu();
        }
        #endregion
    }
}
