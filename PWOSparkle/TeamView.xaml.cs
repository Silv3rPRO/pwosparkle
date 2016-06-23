using PWOBot;
using PWOProtocol;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PWOSparkle
{
    public partial class TeamView : UserControl
    {
        private BotClient _bot;
        private Point _startPoint;

        public TeamView(BotClient bot)
        {
            _bot = bot;
            InitializeComponent();
        }

        private void List_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(null);
        }

        private void List_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = e.GetPosition(null);
            Vector diff = _startPoint - mousePos;

            if (e.LeftButton == MouseButtonState.Pressed &&
                (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                ListView listView = sender as ListView;
                ListViewItem listViewItem = FindAnchestor<ListViewItem>((DependencyObject)e.OriginalSource);

                if (listViewItem != null)
                {
                    Pokemon pokemon = (Pokemon)listView.ItemContainerGenerator.ItemFromContainer(listViewItem);

                    DataObject dragData = new DataObject("PWOSparklePokemon", pokemon);
                    DragDrop.DoDragDrop(listViewItem, dragData, DragDropEffects.Move);
                }
            }
        }

        private static T FindAnchestor<T>(DependencyObject current) where T : DependencyObject
        {
            do
            {
                if (current is T)
                {
                    return (T)current;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            while (current != null);
            return null;
        }

        private void List_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("PWOSparklePokemon"))
            {
                Pokemon sourcePokemon = e.Data.GetData("PWOSparklePokemon") as Pokemon;

                ListView listView = sender as ListView;
                ListViewItem listViewItem = FindAnchestor<ListViewItem>((DependencyObject)e.OriginalSource);

                if (listViewItem != null)
                {
                    Pokemon destinationPokemon = (Pokemon)listView.ItemContainerGenerator.ItemFromContainer(listViewItem);

                    if (_bot.Game != null)
                    {
                        _bot.Game.ReorderPokemon(sourcePokemon.Uid, _bot.Game.Team.IndexOf(destinationPokemon) + 1);
                    }
                }
            }
        }

        private void List_DragEnter(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("PWOSparklePokemon") || sender == e.Source)
            {
                e.Effects = DragDropEffects.None;
            }
        }
    }
}
