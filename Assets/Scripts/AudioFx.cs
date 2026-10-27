using UnityEngine;

public class AudioFx : MonoBehaviour
{
    public static AudioFx Instance { get; private set; }

    AudioSource sfx;
    AudioSource music;
    AudioClip jump, land, attack, heavy, hurt, heal, die, click, checkpoint, clear, drone;

    public static void Play(string name)
    {
        if (Instance != null)
            Instance.PlayNamed(name);
    }

    void Awake()
    {
        Instance = this;
        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        music = gameObject.AddComponent<AudioSource>();
        music.playOnAwake = false;
        music.loop = true;
        music.volume = 0.045f;

        jump = Tone(520f, 0.12f, 0.22f, 180f);
        land = Tone(150f, 0.1f, 0.2f, -40f);
        attack = Noise(0.09f, 0.18f, 900f);
        heavy = Noise(0.16f, 0.22f, 280f);
        hurt = Tone(240f, 0.18f, 0.28f, -180f);
        heal = Chord(new[] { 660f, 880f }, 0.22f, 0.16f);
        die = Tone(320f, 0.45f, 0.24f, -260f);
        click = Tone(880f, 0.05f, 0.15f, 0f);
        checkpoint = Chord(new[] { 523f, 784f }, 0.28f, 0.16f);
        clear = Chord(new[] { 392f, 523f, 659f }, 0.55f, 0.18f);
        drone = Drone();
        music.clip = drone;
        music.Play();
    }

    void PlayNamed(string name)
    {
        AudioClip clip = click;
        switch (name)
        {
            case "jump": clip = jump; break;
            case "land": clip = land; break;
            case "attack": clip = attack; break;
            case "heavy": clip = heavy; break;
            case "hurt": clip = hurt; break;
            case "heal": clip = heal; break;
            case "die": clip = die; break;
            case "checkpoint": clip = checkpoint; break;
            case "clear": clip = clear; break;
        }
        sfx.PlayOneShot(clip);
    }

    static AudioClip Tone(float freq, float duration, float volume, float sweep)
    {
        const int rate = 44100;
        int count = Mathf.Max(1, (int)(rate * duration));
        var data = new float[count];
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)rate;
            float f = freq + sweep * (t / duration);
            float env = Mathf.Exp(-4.2f * t / duration);
            data[i] = Mathf.Sin(2f * Mathf.PI * f * t) * env * volume;
        }
        return Clip(data, rate);
    }

    static AudioClip Chord(float[] freqs, float duration, float volume)
    {
        const int rate = 44100;
        int count = Mathf.Max(1, (int)(rate * duration));
        var data = new float[count];
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)rate;
            float env = Mathf.Exp(-3.2f * t / duration);
            float sample = 0f;
            for (int n = 0; n < freqs.Length; n++)
                sample += Mathf.Sin(2f * Mathf.PI * freqs[n] * t);
            data[i] = sample / freqs.Length * env * volume;
        }
        return Clip(data, rate);
    }

    static AudioClip Noise(float duration, float volume, float cutoff)
    {
        const int rate = 44100;
        int count = Mathf.Max(1, (int)(rate * duration));
        var data = new float[count];
        float prev = 0f;
        float blend = Mathf.Clamp01(cutoff / 4000f);
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)rate;
            float env = Mathf.Exp(-6f * t / duration);
            float n = Random.Range(-1f, 1f);
            prev = Mathf.Lerp(prev, n, blend);
            data[i] = prev * env * volume;
        }
        return Clip(data, rate);
    }

    static AudioClip Drone()
    {
        const int rate = 44100;
        float duration = 8f;
        int count = (int)(rate * duration);
        var data = new float[count];
        float[] freqs = { 110f, 164.81f, 220f };
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)rate;
            float sample = 0f;
            for (int n = 0; n < freqs.Length; n++)
                sample += Mathf.Sin(2f * Mathf.PI * freqs[n] * t);
            float pulse = 0.85f + 0.15f * Mathf.Sin(2f * Mathf.PI * 0.08f * t);
            data[i] = sample / freqs.Length * 0.22f * pulse;
        }
        return Clip(data, rate);
    }

    static AudioClip Clip(float[] data, int rate)
    {
        var clip = AudioClip.Create("fx", data.Length, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
