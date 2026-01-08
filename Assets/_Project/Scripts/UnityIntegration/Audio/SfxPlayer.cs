using AshNCircuit.UnityIntegration.Content;
using UnityEngine;

namespace AshNCircuit.UnityIntegration.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class SfxPlayer : MonoBehaviour
    {
        private AudioSource _audioSource = null!;
        private AudioCatalog _catalog = null!;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.spatialBlend = 0f;
            _catalog = AudioCatalog.LoadFromResources();
        }

        public void PlayOneShot(string sfxId, float volumeScale = 1f)
        {
            var clip = _catalog.GetClipOrThrow(sfxId);
            _audioSource.PlayOneShot(clip, volumeScale);
        }
    }
}
