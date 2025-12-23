using UnityEngine;
using UnityEngine.Video;

namespace UI
{
    [RequireComponent(typeof(VideoPlayer))]
    public class CloudTransition : MonoBehaviour
    {
        private VideoPlayer _videoPlayer;
        private void Awake()
        {
            _videoPlayer = GetComponent<VideoPlayer>();
            LoadingScreen.LoadingDone += SceneLoaded;
            _videoPlayer.Prepare();
        }

        private void SceneLoaded()
        {
            LoadingScreen.LoadingDone -= SceneLoaded;
            _videoPlayer.Play();
        }
    }
}
