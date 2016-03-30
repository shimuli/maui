﻿using System;
using System.Collections.Specialized;
using System.ComponentModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;

#if WINDOWS_UWP

namespace Xamarin.Forms.Platform.UWP
#else

namespace Xamarin.Forms.Platform.WinRT
#endif
{
	public class CarouselPageRenderer : FlipView, IVisualElementRenderer
	{
		bool _fromUpdate;
		bool _disposed;

		BackgroundTracker<FlipView> _tracker;

		public CarouselPageRenderer()
		{
			VirtualizingStackPanel.SetVirtualizationMode(this, VirtualizationMode.Standard);
			ItemTemplate = (Windows.UI.Xaml.DataTemplate)Windows.UI.Xaml.Application.Current.Resources["ContainedPageTemplate"];
			SelectionChanged += OnSelectionChanged;
			Loaded += OnLoaded;
			Unloaded += OnUnloaded;
		}

		public CarouselPage Element { get; private set; }

		public void Dispose()
		{
			Dispose(true);
		}

		public FrameworkElement ContainerElement
		{
			get { return this; }
		}

		VisualElement IVisualElementRenderer.Element
		{
			get { return Element; }
		}

		public event EventHandler<VisualElementChangedEventArgs> ElementChanged;

		public SizeRequest GetDesiredSize(double widthConstraint, double heightConstraint)
		{
			var constraint = new Windows.Foundation.Size(widthConstraint, heightConstraint);

			double oldWidth = Width;
			double oldHeight = Height;

			Height = double.NaN;
			Width = double.NaN;

			Measure(constraint);
			var result = new Size(Math.Ceiling(DesiredSize.Width), Math.Ceiling(DesiredSize.Height));

			Width = oldWidth;
			Height = oldHeight;

			return new SizeRequest(result);
		}

		public void SetElement(VisualElement element)
		{
			var newPage = element as CarouselPage;
			if (element != null && newPage == null)
				throw new ArgumentException("element must be a CarouselPage");

			CarouselPage oldPage = Element;
			Element = newPage;

			if (oldPage != null)
			{
				oldPage.SendDisappearing();
				((INotifyCollectionChanged)oldPage.Children).CollectionChanged -= OnChildrenChanged;
				oldPage.PropertyChanged -= OnElementPropertyChanged;
			}

			if (newPage != null)
			{
				if (_tracker == null)
				{
					_tracker = new BackgroundTracker<FlipView>(BackgroundProperty) { Control = this };
				}

				_tracker.Element = newPage;

				// Adding Items triggers the SelectionChanged event,
				// which will reset the CurrentPage unless we tell it to ignore.
				_fromUpdate = true;
				for (var i = 0; i < newPage.Children.Count; i++)
					Items.Add(newPage.Children[i]);
				_fromUpdate = false;

				((INotifyCollectionChanged)newPage.Children).CollectionChanged += OnChildrenChanged;
				newPage.PropertyChanged += OnElementPropertyChanged;

				UpdateCurrentPage();
				newPage.SendAppearing();
			}

			OnElementChanged(new ElementChangedEventArgs<CarouselPage>(oldPage, newPage));

			if (!string.IsNullOrEmpty(Element?.AutomationId))
				SetValue(AutomationProperties.AutomationIdProperty, Element.AutomationId);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposing || _disposed)
				return;

			if (_tracker != null)
			{
				_tracker.Dispose();
				_tracker = null;
			}

			_disposed = true;
			Element?.SendDisappearing();
			SetElement(null);
		}

		protected virtual void OnElementChanged(ElementChangedEventArgs<CarouselPage> e)
		{
			EventHandler<VisualElementChangedEventArgs> changed = ElementChanged;
			if (changed != null)
				changed(this, new VisualElementChangedEventArgs(e.OldElement, e.NewElement));
		}

		protected virtual void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "CurrentPage")
			{
				UpdateCurrentPage();
			}
		}

		void OnChildrenChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			e.Apply(Element.Children, Items);
		}

		void OnLoaded(object sender, RoutedEventArgs e)
		{
			Element?.SendAppearing();
		}

		void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_fromUpdate)
				return;

			var page = (ContentPage)SelectedItem;
			ContentPage currentPage = Element.CurrentPage;
			if (currentPage == page)
				return;
			currentPage?.SendDisappearing();
			Element.CurrentPage = page;
			page?.SendAppearing();
		}

		void OnUnloaded(object sender, RoutedEventArgs e)
		{
			Element?.SendDisappearing();
		}

		void UpdateCurrentPage()
		{
			_fromUpdate = true;

			SelectedItem = Element.CurrentPage;

			_fromUpdate = false;
		}
	}
}