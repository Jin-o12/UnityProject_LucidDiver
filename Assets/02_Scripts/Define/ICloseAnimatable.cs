using System;

public interface ICloseAnimatable
{
    /// <summary>
    /// 닫기 애니메이션을 실행하고 애니메이션 완료 시 onComplete를 호출해야 합니다.
    /// 반드시 onComplete 를 호출하도록 구현해야 UIManager가 적절히 후처리합니다.
    /// </summary>
    
    /// <param name="onComplete">애니메이션 완료 콜백</param>
    void PlayCloseAnimation(Action onComplete);
}