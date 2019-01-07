using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;

namespace Common.XAML.Controls.HtmlFrameHelpers
{
    /// <summary>
    /// Cette classe contient les outils nécessaires pour les Framework Elements
    /// </summary>
    internal static class FrameworkElementHelpers
    {
        #region public TryGetRootParent Method

        /// <summary>
        /// Cette méthode permet de recherche le parent le plus élevé de l'Element graphique
        /// (normalement, le RootVisual)
        /// </summary>
        /// <param name="element">Element enfant</param>
        /// <param name="parent">Parent</param>
        /// <returns>Vrai si RootVisual, sinon Faux</returns>
        internal static bool TryGetRootParent(FrameworkElement element, out FrameworkElement parent)
        {
            parent = element;
            if (element == null)
            {
                return false;
            }
            else
            {
                parent = element.Parent as FrameworkElement;
                if (Application.Current.RootVisual == parent || parent is ChildWindow)
                {
                    return true;
                }
                else
                {
                    return FrameworkElementHelpers.TryGetRootParent(element.Parent as FrameworkElement, out parent);
                }
            }
        } 

        #endregion

        #region HasParent Method

        /// <summary>
        /// Cette méthode permet de vérifier que l'élément est bien rataché à un parent existant
        /// </summary>
        /// <param name="element">Element Graphique</param>
        /// <returns>Vrai si Parent est RootVisual</returns>
        internal static bool HasParent(FrameworkElement element)
        {
            FrameworkElement parent = null;
            return FrameworkElementHelpers.TryGetRootParent(element, out parent);
        }

        #endregion

        #region HasParentEmptySize Method

        /// <summary>
        /// Cette méthode permet de vérifier que l'élément a bien un parent direct dont la taille existe
        /// </summary>
        /// <param name="element">élément</param>
        /// <returns>Vrai si la parent a une taille </returns>
        internal static bool HasParentEmptySize(FrameworkElement element)
        {
            var parent = element.Parent as FrameworkElement;
            if (parent != null)
            {
                return parent.RenderSize.Equals(new Size(0.0, 0.0));
            }
            else
            {
                return true;
            }
        }

        #endregion
    }
}
