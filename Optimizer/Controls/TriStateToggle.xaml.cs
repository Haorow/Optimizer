using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Color = System.Windows.Media.Color;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace Optimizer.Controls
{
    /// <summary>
    /// Toggle button à 3 états pour la vitesse d'exécution des scripts AHK.
    /// 
    /// États :
    ///   0 = Lent   → fond Bleu        (#296ABF), thumb 10px
    ///   1 = Normal → fond Vert        (#3AAD3A), thumb 20px
    ///   2 = Rapide → fond Jaune orangé (#E88C10), thumb 30px [défaut]
    /// 
    /// Un clic cycle toujours dans l'ordre : Lent → Normal → Rapide → Lent.
    /// Le thumb se "remplit" progressivement de gauche à droite via animation de largeur.
    /// 
    /// Dimensions :
    ///   Fond  : 36×16px, CornerRadius=3
    ///   Thumb : hauteur 10px, largeur animée 10/20/30px, CornerRadius=1.5, Margin=3
    /// 
    /// Usage XAML :
    ///   <controls:TriStateToggle SpeedIndex="{Binding ExecutionSpeedIndex, Mode=TwoWay}"/>
    /// </summary>
    public partial class TriStateToggle : System.Windows.Controls.UserControl
    {
        #region Constantes visuelles

        private static readonly double[] ThumbWidths = { 10.0, 20.0, 30.0 };

        private static readonly Color[] BackgroundColors =
        {
            Color.FromRgb(0x29, 0x6A, 0xBF), // 0 = Lent   → Bleu         #296ABF
            Color.FromRgb(0x3A, 0xAD, 0x3A), // 1 = Normal  → Vert         #3AAD3A
            Color.FromRgb(0xE8, 0x8C, 0x10), // 2 = Rapide  → Jaune orangé #E88C10
        };

        private static readonly Color[] HoverColors =
        {
            Color.FromRgb(0x3A, 0x7F, 0xD4), // 0 = Lent   → Bleu clair    #3A7FD4
            Color.FromRgb(0x4B, 0xC6, 0x4B), // 1 = Normal  → Vert clair    #4BC64B (= checkbox hover)
            Color.FromRgb(0xF0, 0x9D, 0x25), // 2 = Rapide  → Orangé clair  #F09D25
        };

        private static readonly Color[] PressedColors =
        {
            Color.FromRgb(0x1E, 0x54, 0x99), // 0 = Lent   → Bleu foncé    #1E5499
            Color.FromRgb(0x36, 0x99, 0x36), // 1 = Normal  → Vert foncé    #369936 (= checkbox pressed)
            Color.FromRgb(0xC6, 0x7A, 0x0D), // 2 = Rapide  → Orangé foncé  #C67A0D
        };

        private bool _isHovered = false;

        private static readonly TimeSpan AnimDuration = TimeSpan.FromMilliseconds(200);
        private static readonly TimeSpan AnimDurationFast = TimeSpan.FromMilliseconds(80);

        #endregion

        #region DependencyProperty : SpeedIndex

        public static readonly DependencyProperty SpeedIndexProperty =
            DependencyProperty.Register(
                nameof(SpeedIndex),
                typeof(int),
                typeof(TriStateToggle),
                new FrameworkPropertyMetadata(
                    defaultValue: 2,
                    flags: FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    propertyChangedCallback: OnSpeedIndexChanged));

        /// <summary>
        /// Index de vitesse courant : 0 = Lent, 1 = Normal, 2 = Rapide.
        /// Bindable en TwoWay directement sur le ViewModel.
        /// </summary>
        public int SpeedIndex
        {
            get => (int)GetValue(SpeedIndexProperty);
            set => SetValue(SpeedIndexProperty, Math.Clamp(value, 0, 2));
        }

        private static void OnSpeedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TriStateToggle ctrl)
                ctrl.UpdateVisual(animate: true);
        }

        #endregion

        #region Constructeur

        public TriStateToggle()
        {
            InitializeComponent();
            Loaded += (_, _) => UpdateVisual(animate: false);
        }

        #endregion

        #region Interaction

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Cycle : Lent(0) → Normal(1) → Rapide(2) → Lent(0)
            // Seul le clic gauche atteint ce handler (MouseLeftButtonDown en XAML)
            AnimateBackground(PressedColors[Math.Clamp(SpeedIndex, 0, 2)], AnimDurationFast);
            SpeedIndex = (SpeedIndex + 1) % 3;
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            // Après relâchement : repasser en hover puisque la souris est toujours dessus
            AnimateBackground(HoverColors[Math.Clamp(SpeedIndex, 0, 2)], AnimDurationFast);
        }

        private void OnMouseEnter(object sender, MouseEventArgs e)
        {
            _isHovered = true;
            AnimateBackground(HoverColors[Math.Clamp(SpeedIndex, 0, 2)], AnimDurationFast);
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            _isHovered = false;
            AnimateBackground(BackgroundColors[Math.Clamp(SpeedIndex, 0, 2)], AnimDurationFast);
        }

        #endregion

        #region Animation visuelle

        /// <summary>
        /// Anime la largeur du thumb et la couleur du fond selon SpeedIndex.
        /// Tient compte de l'état hover courant pour la couleur cible.
        /// </summary>
        private void UpdateVisual(bool animate)
        {
            int idx = Math.Clamp(SpeedIndex, 0, 2);
            double targetWidth = ThumbWidths[idx];
            Color targetColor = _isHovered ? HoverColors[idx] : BackgroundColors[idx];

            var duration = new Duration(animate ? AnimDuration : TimeSpan.Zero);
            var ease = new QuadraticEase { EasingMode = EasingMode.EaseOut };

            // ── Animation de la largeur du thumb ───────────────────────────────
            var widthAnim = new DoubleAnimation(targetWidth, duration) { EasingFunction = ease };
            SwitchThumb.BeginAnimation(WidthProperty, widthAnim);

            // ── Animation de la couleur de fond ────────────────────────────────
            AnimateBackground(targetColor, animate ? AnimDuration : TimeSpan.Zero);
        }

        /// <summary>
        /// Anime uniquement la couleur de fond vers la couleur cible.
        /// </summary>
        private void AnimateBackground(Color targetColor, TimeSpan duration)
        {
            if (SwitchBackground.Background is SolidColorBrush brush)
            {
                if (brush.IsFrozen)
                {
                    var mutableBrush = new SolidColorBrush(brush.Color);
                    SwitchBackground.Background = mutableBrush;
                    brush = mutableBrush;
                }

                var colorAnim = new ColorAnimation(targetColor, new Duration(duration));
                brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnim);
            }
            else
            {
                SwitchBackground.Background = new SolidColorBrush(targetColor);
            }
        }

        #endregion
    }
}