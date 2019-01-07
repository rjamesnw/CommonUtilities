using System;
using System.ComponentModel;
using System.Net;
using System.Windows;
using System.Windows.Browser;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Common.XAML.Controls.HtmlFrameHelpers;

namespace Common.XAML.Controls
{
    /// <summary>
    /// Cette classe représente une IFrame Web dans laquelle il est possible d'intégrer une application Web au sein d'une application Silverlight
    /// Attention ! L'objet Silverlight doit être : name="Windowless" value="true"
    /// </summary>
    public class HtmlFrame : ContentControl, IDisposable
    {
        #region Private Members

        /// <summary>
        /// Objet Html div
        /// </summary>
        private HtmlElement div;

        /// <summary>
        /// Objet Html IFrame
        /// </summary>
        private HtmlElement iFrame;

        /// <summary>
        /// Affiche ou non le contenu (gérer par la changement dans le Layout)
        /// </summary>
        private Visibility internalVisibility = Visibility.Collapsed;

        /// <summary>
        /// Positionnement de la Frame HTML
        /// </summary>
        private Point internalPosition = new Point(0.0, 0.0);

        /// <summary>
        /// Element Root principal
        /// </summary>
        private FrameworkElement rootParent = null;

        /// <summary>
        /// Element Root contenu dans un ChildWindow (Initialiser par l'intégration dans un ChildWindow
        /// </summary>
        private FrameworkElement contentChildWindow;

        /// <summary>
        /// Element ChildWindow
        /// </summary>
        private FrameworkElement chromeChildWindow;

        /// <summary>
        /// Définit si le premier affichage est réalisé
        /// </summary>
        private bool isLoaded;

        #endregion

        #region NavigateUrl DependencyProperty

        /// <summary>
        /// Url du Site Web
        /// </summary>
        public Uri NavigateUrl
        {
            get { return (Uri)GetValue(NavigateUrlProperty); }
            set { SetValue(NavigateUrlProperty, value); }
        }

        /// <summary>
        /// Url du Site Web
        /// Using a DependencyProperty as the backing store for NavigateUrl.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty NavigateUrlProperty = DependencyProperty.Register("NavigateUrl", typeof(Uri), typeof(HtmlFrame), new PropertyMetadata(null, new PropertyChangedCallback(OnNavigateUrlChanged)));

        /// <summary>
        /// Cette méthode permet de gérer le changement de l'URL
        /// </summary>
        /// <param name="dep">HtmlFrame</param>
        /// <param name="e">Arguement de changement</param>
        private static void OnNavigateUrlChanged(DependencyObject dep, DependencyPropertyChangedEventArgs e)
        {
            var sender = dep as HtmlFrame;
            if (sender != null)
            {
                sender.UpadeHtmlContent();
            }
        }

        #endregion
        
        #region InnerHtml DependencyProperty

        /// <summary>
        /// Contenu Html de la Frame
        /// </summary>
        public string InnerHtml
        {
            get { return (string)GetValue(InnerHtmlProperty); }
            set { SetValue(InnerHtmlProperty, value); }
        }

        /// <summary>
        /// Contenu Html de la Frame
        /// Using a DependencyProperty as the backing store for InnerHtml.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty InnerHtmlProperty = DependencyProperty.Register("InnerHtml", typeof(string), typeof(HtmlFrame), new PropertyMetadata("", new PropertyChangedCallback(OnInnerHtmlChanged)));

        /// <summary>
        /// Cette méthode permet de gérer le changement du contenu Html
        /// </summary>
        /// <param name="dep">HtmlFrame</param>
        /// <param name="e">Arguement de changement</param>
        private static void OnInnerHtmlChanged(DependencyObject dep, DependencyPropertyChangedEventArgs e)
        {
            var sender = dep as HtmlFrame;
            if (sender != null)
            {
                sender.UpadeHtmlContent();
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructeur par défaut
        /// </summary>
        public HtmlFrame()
        {
            this.isLoaded = false;
        }

        #endregion

        #region OnApplyTemplate Method

        /// <summary>
        /// Cette méthode applique le template du Control
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.CreateChildControls();

            // Permet de récupérer le changement de la visibilité
            this.LayoutUpdated += new EventHandler(this.OnLayoutUpdated);

            // Permet de récupérer le redimentionnement du panel
            this.SizeChanged += new SizeChangedEventHandler(this.OnSizeChanged);

            // Récupération du Parent principal
            if (!FrameworkElementHelpers.TryGetRootParent(this, out rootParent))
            {
                HtmlPage.Document.Body.RemoveChild(this.div);
                this.internalVisibility = Visibility.Collapsed;
            }

            // Initialisation pour le traitement d'un ChildWindow
            this.InitializeChildWindow();

        }

        #endregion

        #region CreateChildControls Method

        /// <summary>
        /// Cette méthode permet de créer l'ensemble des controls HTML composant ce dernier
        /// </summary>
        void CreateChildControls()
        {
            string baseId = this.Name;
            if (string.IsNullOrEmpty(baseId))
            {
                baseId = Guid.NewGuid().ToString();
            }

            HtmlDocument htmlDocument = HtmlPage.Document;
            this.div = htmlDocument.CreateElement("div");
            this.div.Id = this.Name + "_BrowserDiv";
            this.div.SetStyleAttribute("position", "absolute");

            this.iFrame = htmlDocument.CreateElement("iframe");
            this.iFrame.Id = this.Name + "_BrowserIFrame";
            this.iFrame.SetProperty("frameborder", "no");
            this.iFrame.SetStyleAttribute("position", "relative");
            this.iFrame.SetProperty("src", "blank.html");

            this.UpadeHtmlContent();
            
            if (this.Visibility.Equals(Visibility.Visible))
            {
                HtmlPage.Document.Body.AppendChild(this.div);
            }

        }

        #endregion

        #region OnLayoutUpdated Method

        /// <summary>
        /// Cette méthode permet de traiter le changement de visibilité sur le control
        /// </summary>
        /// <param name="sender">SilverlightBrowser</param>
        /// <param name="e">empty</param>
        void OnLayoutUpdated(object sender, EventArgs e)
        {
            // Vérifie que la taille du existe 
            if (FrameworkElementHelpers.HasParentEmptySize(this))
            {
                HtmlPage.Document.Body.RemoveChild(this.div);
                this.internalVisibility = Visibility.Collapsed;
                this.RemoveChromeChildWindowHandler();
            }
            else
            {
                if (!this.internalVisibility.Equals(this.Visibility))
                {
                    this.internalVisibility = this.Visibility;
                    if (this.internalVisibility.Equals(Visibility.Visible))
                    {
                        HtmlPage.Document.Body.AppendChild(this.div);
                        this.AddChromeChildWindowHandler();
                        this.ChangePosition();
                    }
                    else
                    {
                        HtmlPage.Document.Body.RemoveChild(this.div);
                        this.RemoveChromeChildWindowHandler();
                    }
                }
            }
        }

        #endregion

        #region UpadeHtmlContent Method

        /// <summary>
        /// Cette méthode permet de mettre à jour de contenu HTML de la Div / IFrame
        /// </summary>
        void UpadeHtmlContent()
        {
            if (this.div != null && this.iFrame != null)
            {
                if (this.NavigateUrl != null)
                {
                    //this.iFrame.SetAttribute("src", this.NavigateUrl.AbsoluteUri);
                    this.iFrame.SetProperty("src", this.NavigateUrl.AbsoluteUri);
                    this.div.SetAttribute("innerHTML", "");
                    this.div.AppendChild(iFrame);
                }
                else
                {
                    this.div.RemoveChild(iFrame);
                    this.div.SetAttribute("innerHTML", this.InnerHtml);
                }
            }
        }

        #endregion

        #region OnSizeChanged Method

        /// <summary>
        /// Cette méthode permet de traiter le changement de taille du control
        /// </summary>
        /// <param name="sender">SilverlightBrowser</param>
        /// <param name="e">Nouvelle taille</param>
        void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.div.SetStyleAttribute("height", e.NewSize.Height.ToString() + "px");
            this.div.SetStyleAttribute("width", e.NewSize.Width.ToString() + "px");
            this.iFrame.SetStyleAttribute("height", e.NewSize.Height.ToString() + "px");
            this.iFrame.SetStyleAttribute("width", e.NewSize.Width.ToString() + "px");
        }

        #endregion

        #region ChangePosition Method

        /// <summary>
        /// Cette méthode permet de changer la position de la Frame HTML
        /// </summary>
        void ChangePosition()
        {
            // Récupére la position par rapport au root Visuel
            GeneralTransform gt = this.TransformToVisual(Application.Current.RootVisual);
            var positionPoint = gt.Transform(new Point(0, 0));

            // Applique l'OffSet
            var offSetPosition = this.GetOriginalChildWindowOffSet();
            var newPosition = new Point(positionPoint.X - offSetPosition.X, positionPoint.Y - offSetPosition.Y);

            // Affectation de la nouvelle position
            if (newPosition != this.internalPosition)
            {
                this.internalPosition = newPosition;
                this.div.SetStyleAttribute("left", this.internalPosition.X + "px");
                this.div.SetStyleAttribute("top", this.internalPosition.Y + "px");
            }
        } 

        #endregion

        #region ChildWindow Methods

        /// <summary>
        /// Cette méthode permet d'initialiser le parent ChildWindow
        /// (Pour gérer le déplacement de la fenetre)
        /// </summary>
        void InitializeChildWindow()
        {
            if (rootParent is ChildWindow)
            {
                FrameworkElement root = VisualTreeHelper.GetChild(rootParent, 0) as FrameworkElement;
                if (root != null)
                {
                    this.contentChildWindow = root.FindName("ContentRoot") as FrameworkElement;
                    this.chromeChildWindow = root.FindName("Chrome") as FrameworkElement;
                    this.AddChromeChildWindowHandler();
                }
            }            
        }

        /// <summary>
        /// Cette méthode permet de calculer le décallage d'origine de la ChildWindow
        /// </summary>
        /// <returns>Position OffSet</returns>
        Point GetOriginalChildWindowOffSet()
        {
            var result = new Point(0.0, 0.0);

            // TODO !! A SUPPRIMER CAR SUREMENT DU A l'OUVERTURE DE LA CHILDWINDOW

            // Calcul de l'Offset de démarrage
            //if (this.rootParent is ChildWindow && !this.isLoaded)
            //{
            //    this.isLoaded = true;
            //    GeneralTransform contentTransform = this.TransformToVisual(this.contentChildWindow);
            //    var contentOffSetPoint = contentTransform.Transform(new Point(0, 0));
            //    result = new Point(this.contentChildWindow.Width / 2 - contentOffSetPoint.X, this.contentChildWindow.Height / 2 - contentOffSetPoint.Y);
            //}

            return result;
        }

        /// <summary>
        /// Cette méthode ajoute la gestion du Move sur le ChildWindow
        /// </summary>
        void AddChromeChildWindowHandler()
        {
            if (this.chromeChildWindow != null)
            {
                this.chromeChildWindow.MouseMove += new MouseEventHandler(this.ChildWindowMouseMove);
            }
        }

        /// <summary>
        /// Cette méthode supprime la gestion du Move sur le ChildWindow
        /// </summary>
        void RemoveChromeChildWindowHandler()
        {
            if (this.chromeChildWindow != null)
            {
                this.chromeChildWindow.MouseMove -= this.ChildWindowMouseMove;
            }
        }

        /// <summary>
        /// Cette méthode permet de gérer le déplaction d'un ChildWindow Parent
        /// </summary>
        /// <param name="sender">Parent ChildWindow</param>
        /// <param name="e">Mouse Argument</param>
        void ChildWindowMouseMove(object sender, MouseEventArgs e)
        {
            this.ChangePosition();
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Cette méthode permet de disposer de controle
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
        }
        
        /// <summary>
        /// Cette méthode permet de disposer de controle
        /// </summary>
        /// <param name="disposing">Demande la disposition</param>
        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.RemoveChromeChildWindowHandler();
            }
        }

        #endregion
    }
}
