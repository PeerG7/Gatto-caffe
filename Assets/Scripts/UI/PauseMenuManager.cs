using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuManager : MonoBehaviour
{
    [Header("UI Canvas / Panel References")]
    [Tooltip("ลาก GameObject ของ Pause Menu Panel ทั้งหมดมาใส่ที่นี่ (สคริปต์นี้ต้อง 'ไม่' อยู่บน Panel นี้หรือลูกของมัน)")]
    public GameObject pauseMenuUI;

    [Tooltip("ลาก GameObject ของ Panel ยืนยันการออกเกม (Exit Confirmation) มาใส่ที่นี่")]
    public GameObject confirmExitPanel;

    [Header("Scene Settings")]
    [Tooltip("ชื่อของ Scene หน้า Main Menu ที่จะให้กลับไปเมื่อกด Title")]
    public string mainMenuSceneName = "MainMenu";

    // 🟢 FIX: ใช้ตัวแปรของเราเองบอกสถานะเมนู แทนการเช็ก pauseMenuUI.activeSelf
    // (กัน state เพี้ยน และกัน ResumeGame() ไปลดตัวนับ Pause ของระบบอื่นเช่น Tutorial)
    private bool _isMenuOpen = false;

    void Start()
    {
        // 🟢 เตือนถ้าสคริปต์นี้อยู่บน Panel ที่ถูกปิด — Update() จะหยุดทำงานและ ESC จะไม่ตอบสนองอีกเลย
        if (pauseMenuUI != null && (pauseMenuUI == gameObject || transform.IsChildOf(pauseMenuUI.transform)))
        {
            Debug.LogError("[PauseMenuManager] สคริปต์นี้อยู่บน/ใต้ pauseMenuUI! เมื่อ Panel ถูกปิด Update() จะหยุดทำงาน ESC จะเปิดเมนูไม่ได้ " +
                           "ให้ย้ายสคริปต์ไปไว้ใน GameObject แยก (เช่น PauseManager) ที่ไม่ถูกปิด");
        }

        _isMenuOpen = false;
        Time.timeScale = 1f; // 🟢 กัน timeScale ค้างจากฉากก่อนหน้า

        // ปิดหน้า Pause และบังคับปิดหน้าต่างยืนยันการออกตั้งแต่เริ่มเกมเสมอ
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (confirmExitPanel != null) confirmExitPanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // ถ้าหน้าต่างยืนยันออกเกมเปิดอยู่ ให้กด Esc เพื่อปิดหน้าต่างยืนยันนั้นก่อน
            if (confirmExitPanel != null && confirmExitPanel.activeSelf)
            {
                CloseConfirmExit();
                return;
            }

            if (pauseMenuUI == null) return;

            if (_isMenuOpen) ResumeGame();
            else PauseGame();
        }
    }

    /// <summary>
    /// หยุดเกมและแสดงหน้า Pause Menu
    /// </summary>
    public void PauseGame()
    {
        if (_isMenuOpen) return; // กันเรียกซ้ำแล้วตัวนับ Pause เพิ่มเกิน
        _isMenuOpen = true;

        if (pauseMenuUI != null) pauseMenuUI.SetActive(true);
        if (confirmExitPanel != null) confirmExitPanel.SetActive(false);

        if (DayNightManager.Instance != null)
        {
            DayNightManager.Instance.PauseGame();
        }
        else
        {
            Time.timeScale = 0f; // Fallback เผื่อกรณีไม่มี DayNightManager ในฉาก
        }
    }

    /// <summary>
    /// ปุ่ม Continue: เล่นเกมต่อจากปัจจุบัน (เลิก Pause)
    /// </summary>
    public void ResumeGame()
    {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (confirmExitPanel != null) confirmExitPanel.SetActive(false);

        if (!_isMenuOpen) return; // เมนูไม่ได้เป็นคน Pause → ห้าม Resume (ไม่งั้นไปลดตัวนับของ Tutorial)
        _isMenuOpen = false;

        if (DayNightManager.Instance != null)
        {
            DayNightManager.Instance.ResumeGame();
        }
        else
        {
            Time.timeScale = 1f;
        }
    }

    /// <summary>
    /// ปุ่ม Options: มีไว้เฉยๆ ก่อน
    /// </summary>
    public void OnOptionsPressed()
    {
        Debug.Log("⚙️ Options button clicked (ยังไม่มีฟังก์ชันในตอนนี้)");
    }

    /// <summary>
    /// ปุ่ม Title: กลับหน้า Main Menu (ไม่เซฟกลางวัน — Continue จะกลับไปจุดเริ่มต้นของวันที่เซฟไว้ล่าสุด)
    /// </summary>
    public void OnTitlePressed()
    {
        if (DayNightManager.Instance != null)
        {
            DayNightManager.Instance.ForceResume();
        }
        _isMenuOpen = false;
        Time.timeScale = 1f;

        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void OnExitPressed()
    {
        if (confirmExitPanel != null)
        {
            confirmExitPanel.SetActive(true);
        }
    }

    public void CloseConfirmExit()
    {
        if (confirmExitPanel != null)
        {
            confirmExitPanel.SetActive(false);
        }
    }

    public void ConfirmExitGame()
    {
        // ไม่เซฟกลางวัน (ดูคำอธิบายที่ OnTitlePressed)
        Debug.Log("🚪 กำลังออกจากเกม...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}