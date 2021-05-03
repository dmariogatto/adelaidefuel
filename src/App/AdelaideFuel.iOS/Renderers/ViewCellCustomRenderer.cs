using AdelaideFuel.iOS.Renderers;
using Foundation;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(ViewCell), typeof(ViewCellCustomRenderer))]
namespace AdelaideFuel.iOS.Renderers
{
    [Preserve(AllMembers = true)]
    public class ViewCellCustomRenderer : ViewCellRenderer
    {
        public override UITableViewCell GetCell(Cell item, UITableViewCell reusableCell, UITableView tv)
        {
            var cell = base.GetCell(item, reusableCell, tv);
            // Disable selection color when tapping in iOS
            cell.SelectionStyle = UITableViewCellSelectionStyle.None;
            return cell;
        }
    }
}