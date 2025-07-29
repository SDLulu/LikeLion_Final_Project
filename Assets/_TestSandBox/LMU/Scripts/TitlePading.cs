using LMCore;
using UnityEngine;

public class TitlePading : MonoBehaviour
{
    public async void Awake()
    {
        await Fader.Inst.FadeOutAsync(Color.black, 3.0f);
    }

    public void PlaySound()
    {
    }
}
