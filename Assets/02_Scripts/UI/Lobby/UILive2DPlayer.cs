using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[RequireComponent(typeof(RawImage))]
public class UILive2DPlayer : MonoBehaviour
{
    [Header("Live2D Settings")]
    [SerializeField] private VideoClip videoClip;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool loop = true;

    private VideoPlayer videoPlayer;
    private RenderTexture videoRenderTexture;
    private RawImage rawImage;

    private void Awake()
    {
        rawImage = GetComponent<RawImage>();
    }

    private void OnEnable()
    {
        if (playOnStart)
        {
            PlayVideo();
        }
    }

    private void OnDisable()
    {
        StopVideo();
    }


    /// <summary>
    /// 재생할 비디오 클립을 지정하여 재생을 구동합니다.
    /// </summary>
    public void PlayVideo(VideoClip clip = null)
    {
        if (clip != null)
        {
            videoClip = clip;
        }

        if (videoClip == null || rawImage == null) return;

        // 기존 재생 리소스 해제
        StopVideo();

        // VideoPlayer 컴포넌트 확보
        videoPlayer = gameObject.GetComponent<VideoPlayer>();
        if (videoPlayer == null)
        {
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
        }

        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = videoClip;
        videoPlayer.isLooping = loop;
        videoPlayer.playOnAwake = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;

        // 비디오 해상도 규격에 맞춰 알파 채널(ARGB32)을 포함하는 RenderTexture 생성
        videoRenderTexture = new RenderTexture((int)videoClip.width, (int)videoClip.height, 0, RenderTextureFormat.ARGB32);
        videoRenderTexture.Create();

        videoPlayer.targetTexture = videoRenderTexture;
        rawImage.texture = videoRenderTexture;
        rawImage.color = Color.white; // 색 필터 초기화

        videoPlayer.Play();
    }

    /// <summary>
    /// 재생을 중지하고 RenderTexture 리소스를 정리합니다.
    /// </summary>
    public void StopVideo()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }

        if (videoRenderTexture != null)
        {
            videoRenderTexture.Release();
            Destroy(videoRenderTexture);
            videoRenderTexture = null;
        }
    }

    private void OnDestroy()
    {
        StopVideo();
    }
}
