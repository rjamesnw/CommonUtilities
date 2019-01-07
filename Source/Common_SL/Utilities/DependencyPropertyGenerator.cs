using System;
using System.Windows;

namespace OneMenu
{
    public partial class IconMenuItem  
    {

        #region Icon
        
        /// <summary>
        /// The image used as the icon for this item (see 'Configure()').<para>(This is a Dependency Property)</para>
        /// <summary>    
        public System.Windows.Controls.Image Icon
        {
            get { return (System.Windows.Controls.Image)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }
    
        /// <summary>
        /// Identifies the Icon Dependency Property.
        /// <summary>
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register("Icon", typeof(System.Windows.Controls.Image),
            typeof(IconMenuItem), new PropertyMetadata(null, OnIconPropertyChanged));
    
    	
        private static void OnIconPropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            IconMenuItem myClass = d as IconMenuItem;
            
            myClass.OnIconPropertyChanged(e);
        }
    
        partial void OnIconPropertyChanged(DependencyPropertyChangedEventArgs e);
        
            
        #endregion
    
        #region Description
        
        /// <summary>
        /// The icon description used for this item (see 'Configure()').<para>(This is a Dependency Property)</para>
        /// <summary>    
        public string Description
        {
            get { return (string)GetValue(DescriptionProperty); }
            set { SetValue(DescriptionProperty, value); }
        }
    
        /// <summary>
        /// Identifies the Description Dependency Property.
        /// <summary>
        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register("Description", typeof(string),
            typeof(IconMenuItem), new PropertyMetadata(null, OnDescriptionPropertyChanged));
    
    	
        private static void OnDescriptionPropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            IconMenuItem myClass = d as IconMenuItem;
            
            myClass.OnDescriptionPropertyChanged(e);
        }
    
        partial void OnDescriptionPropertyChanged(DependencyPropertyChangedEventArgs e);
        
            
        #endregion
    
        #region ItemID
        
        /// <summary>
        /// A unique text-based ID used for this item to help identify it when selected.<para>(This is a Dependency Property)</para>
        /// <summary>    
        public string ItemID
        {
            get { return (string)GetValue(ItemIDProperty); }
            set { SetValue(ItemIDProperty, value); }
        }
    
        /// <summary>
        /// Identifies the ItemID Dependency Property.
        /// <summary>
        public static readonly DependencyProperty ItemIDProperty =
            DependencyProperty.Register("ItemID", typeof(string),
            typeof(IconMenuItem), new PropertyMetadata(null, OnItemIDPropertyChanged));
    
    	
        private static void OnItemIDPropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            IconMenuItem myClass = d as IconMenuItem;
            
            myClass.OnItemIDPropertyChanged(e);
        }
    
        partial void OnItemIDPropertyChanged(DependencyPropertyChangedEventArgs e);
        
            
        #endregion
     
    }
}

