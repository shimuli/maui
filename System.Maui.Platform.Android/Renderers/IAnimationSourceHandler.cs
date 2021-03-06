using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.Graphics.Drawables;

namespace System.Maui.Platform.Android
{
	public interface IAnimationSourceHandler : IRegisterable
	{
		Task<IFormsAnimationDrawable> LoadImageAnimationAsync(ImageSource imagesource, Context context, CancellationToken cancelationToken = default(CancellationToken), float scale = 1);
	}
}