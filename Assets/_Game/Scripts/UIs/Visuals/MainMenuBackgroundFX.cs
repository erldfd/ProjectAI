using UnityEngine;

namespace ProjectAI.UIs.Visuals
{
    /// <summary>
    /// 외부 애셋 없이 유니티 내장 파티클만으로 메인 메뉴의 시네마틱 배경 연출(빛무리/불티)을 생성합니다.
    /// </summary>
    public class MainMenuBackgroundFX : MonoBehaviour
    {
        private ParticleSystem ps;

        private void Awake()
        {
            CreateParticleSystem();
        }

        private void OnEnable()
        {
            if (ps != null)
            {
                ps.Play();
            }
        }

        private void OnDisable()
        {
            if (ps != null)
            {
                ps.Stop();
            }
        }

        private void CreateParticleSystem()
        {
            // 파티클을 담을 빈 게임 오브젝트 생성
            GameObject fxObj = new GameObject("MainMenu_BackgroundParticles");
            fxObj.transform.SetParent(this.transform);
            
            // 카메라 시야(앞) 쪽에 배치하되 약간 아래에서 위로 올라오도록 설정
            fxObj.transform.localPosition = new Vector3(0, -10f, 50f);
            
            ps = fxObj.AddComponent<ParticleSystem>();
            
            // AddComponent 순간 자동 재생되므로 설정 변경을 위해 우선 멈춤
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            
            // 7. Renderer (URP 호환 셰이더 강제 할당)
            ParticleSystemRenderer renderer = fxObj.GetComponent<ParticleSystemRenderer>();
            Shader urpShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (urpShader != null)
            {
                Material urpMat = new Material(urpShader);
                // 블렌드 모드 설정 (투명/Additive 느낌)
                urpMat.SetFloat("_Surface", 1); // 1 = Transparent
                urpMat.SetFloat("_Blend", 1);   // 1 = Additive
                renderer.sharedMaterial = urpMat;
            }
            
            // 1. Main 모듈 (수명, 크기, 속도)
            ParticleSystem.MainModule main = ps.main;
            main.playOnAwake = false;
            main.duration = 5f;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 12f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
            main.startColor = new Color(0.3f, 0.6f, 1f, 0.6f); // 은은한 푸른빛
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;

            // 2. Emission 모듈 (발생량)
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 15f; // 잔잔하게 흩날림

            // 3. Shape 모듈 (발생 범위)
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(40f, 1f, 10f); // 화면 너비만큼 넓게 배치

            // 4. Velocity Over Lifetime (위로 천천히 떠오름)
            ParticleSystem.VelocityOverLifetimeModule velOverLifetime = ps.velocityOverLifetime;
            velOverLifetime.enabled = true;
            velOverLifetime.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            velOverLifetime.y = new ParticleSystem.MinMaxCurve(0.2f, 1.0f);
            velOverLifetime.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            // 5. Color Over Lifetime (알파 페이드 인/아웃)
            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(0.2f, 0.8f, 1f), 0f), new GradientColorKey(new Color(0f, 0.3f, 1f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.5f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

            // 6. Noise (바람에 흔들리는 느낌)
            ParticleSystem.NoiseModule noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.3f;
            noise.frequency = 0.5f;
            noise.scrollSpeed = 0.2f;

            // 모든 셋업 완료 후 재생
            ps.Play();
        }
    }
}
