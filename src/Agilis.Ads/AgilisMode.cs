namespace Agilis.Ads;

/// <summary>
/// The mode an API key works in. A <c>pk_test_</c> key is <see cref="Test"/> and a
/// <c>pk_live_</c> key is <see cref="Live"/>.
/// </summary>
public enum AgilisMode
{
    /// <summary>Test mode: members never see what is created and nothing real is charged.</summary>
    Test,

    /// <summary>Live mode: changes are real and paid from the organisation's balance.</summary>
    Live,
}
