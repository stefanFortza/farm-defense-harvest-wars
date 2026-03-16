using Godot;

public static class UIExtensions
{
	// Culoarea de "Hover"
	private static readonly Color HoverColor = new(1.2f, 1.2f, 1.2f, 1.0f);

	public static Tween AnimatePosition(this Control node, Vector2 targetPos, float duration = 0.2f)
	{
		var tween = node.CreateTween().SetParallel(true);
		tween.TweenProperty(node, "position", targetPos, duration)
			 .SetTrans(Tween.TransitionType.Sine)
			 .SetEase(Tween.EaseType.Out);

		// Adăugăm și luminare
		// tween.TweenProperty(node, "modulate", HoverColor, duration);

		return tween;
	}

	public static Tween ResetPosition(this Control node, Vector2 originalPos, float duration = 0.2f)
	{
		var tween = node.CreateTween().SetParallel(true);
		tween.TweenProperty(node, "position", originalPos, duration)
			 .SetTrans(Tween.TransitionType.Sine)
			 .SetEase(Tween.EaseType.Out);

		// tween.TweenProperty(node, "modulate", Colors.White, duration);

		return tween;
	}

	public static Tween Shake(this Control node, float angle = 15.0f, float duration = 0.3f)
	{
		node.PivotOffset = node.Size / 2; // Shake-ul arată totuși mai bine din centru
		var tween = node.CreateTween();
		tween.TweenProperty(node, "rotation_degrees", angle, duration * 0.33f).SetTrans(Tween.TransitionType.Sine);
		tween.TweenProperty(node, "rotation_degrees", -angle, duration * 0.33f).SetTrans(Tween.TransitionType.Sine);
		tween.TweenProperty(node, "rotation_degrees", 0, duration * 0.34f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);

		return tween;
	}

	public static Tween ResetShake(this Control node)
	{
		node.PivotOffset = node.Size / 2;
		var tween = node.CreateTween();
		tween.TweenProperty(node, "rotation_degrees", 0, 0.1f);

		return tween;
	}
}