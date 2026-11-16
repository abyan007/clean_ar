using UnityEngine;

public class DragAndThrow : MonoBehaviour
{
    private Rigidbody rb;
    private Camera arCam;
    private Vector2 posisiAwalSentuh;
    private Vector3 posisiLokalAwal;
    private Vector3 skalaAwal;
    private Transform parentAwal;
    private float jarakZ;
    private bool sedangDitarik = false;
    private bool sudahMasukTong = false;

    [Header("Pengaturan Tong Sampah")]
    public Transform targetTongSampah;
    public float kekuatanLempar = 4f;
    [Range(0.1f, 1f)]
    public float rasioSkalaDiTong = 0.45f; // Mengatur ukuran kertas agar muat di dalam tong

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        arCam = Camera.main;
        parentAwal = transform.parent;
        posisiLokalAwal = transform.localPosition;
        skalaAwal = transform.localScale;
        rb.isKinematic = true;
    }

    void Update()
    {
        if (sudahMasukTong) return; // Kertas diam di dalam tong jika sudah masuk

        if (Input.touchCount > 0)
        {
            Touch sentuhan = Input.GetTouch(0);

            if (sentuhan.phase == TouchPhase.Began)
            {
                Ray ray = arCam.ScreenPointToRay(sentuhan.position);
                RaycastHit[] semuaHit = Physics.RaycastAll(ray, 100f);

                foreach (RaycastHit hit in semuaHit)
                {
                    if (hit.transform == transform)
                    {
                        sedangDitarik = true;
                        rb.isKinematic = true;
                        jarakZ = arCam.WorldToScreenPoint(transform.position).z;
                        posisiAwalSentuh = sentuhan.position;
                        break;
                    }
                }
            }
            else if ((sentuhan.phase == TouchPhase.Moved || sentuhan.phase == TouchPhase.Stationary) && sedangDitarik)
            {
                float kedalamanAktif = jarakZ;

                if (targetTongSampah != null && sentuhan.position.y < Screen.height * 0.45f)
                {
                    kedalamanAktif = arCam.WorldToScreenPoint(targetTongSampah.position).z;
                    // Perkecil skala kertas agar tidak tampak raksasa saat mendekati kamera/tong
                    transform.localScale = skalaAwal * rasioSkalaDiTong;
                }
                else
                {
                    transform.localScale = skalaAwal;
                }

                Vector3 posisiLayar = new Vector3(sentuhan.position.x, sentuhan.position.y, kedalamanAktif);
                transform.position = arCam.ScreenToWorldPoint(posisiLayar);
            }
            else if ((sentuhan.phase == TouchPhase.Ended || sentuhan.phase == TouchPhase.Canceled) && sedangDitarik)
            {
                sedangDitarik = false;
                Vector2 arahGeser = sentuhan.position - posisiAwalSentuh;

                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;

                if (arahGeser.y > 150f)
                {
                    Vector3 arahLempar = (arCam.transform.forward * 0.5f + Vector3.up * 0.3f).normalized;
                    float tenaga = Mathf.Clamp(arahGeser.magnitude / Screen.height, 0.2f, 0.8f);
                    rb.AddForce(arahLempar * tenaga * kekuatanLempar, ForceMode.Impulse);
                }

                // Kembalikan ke meja hanya jika meleset setelah 2.5 detik
                Invoke(nameof(ResetKertas), 2.5f);
            }
        }
    }

    void ResetKertas()
    {
        if (sudahMasukTong) return;

        CancelInvoke(nameof(ResetKertas));
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        transform.SetParent(parentAwal);
        transform.localPosition = posisiLokalAwal;
        transform.localScale = skalaAwal;
    }

    void OnTriggerEnter(Collider other)
    {
        if (sudahMasukTong) return;

        if (other.CompareTag("TempatSampah"))
        {
            if (transform.position.y >= other.transform.position.y - 0.05f)
            {
                sedangDitarik = false;
                sudahMasukTong = true;
                CancelInvoke(nameof(ResetKertas));

                // Matikan fisika agar kertas tidak tembus jatuh keluar tong
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;

                // Tempelkan kertas menjadi bagian dari tong sampah di bagian dasar
                transform.SetParent(other.transform);
                transform.localScale = skalaAwal * rasioSkalaDiTong;
                transform.localPosition = new Vector3(0f, -0.7f, 0f); // Duduk di dasar dalam tong
            }
        }
    }
}