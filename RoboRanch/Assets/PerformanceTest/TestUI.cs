using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TestUI : MonoBehaviour
{
    public GameObject[] TestObjects;
    public int Column;
    public float ObjectSize;
    public CameraMultiTarget Camera;
    
    private int CurrentColumn, CurrentRow;

    private GameObject _selectedObject;
    Dictionary<GameObject, List<GameObject>> _objects = new Dictionary<GameObject, List<GameObject>>();
    
    public float timer, refresh, avgFramerate;
    string display = "FPS: {0}";
    public TextMeshProUGUI m_Text;
    public TMP_InputField _input;
    public TMP_Dropdown _dropdown;

    void Start()
    {
        _input.onEndEdit.AddListener(OnNumberEntered);
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();

        for (int i = 0; i < TestObjects.Length; i++)
        {
            options.Add(new TMP_Dropdown.OptionData(TestObjects[i].name));
        }
        
        _dropdown.AddOptions(options);
        _dropdown.onValueChanged.AddListener(OnObjectSelected);
        
        OnObjectSelected(0);
    }

    void OnObjectSelected(int objectNum)
    {
        if(_selectedObject != null &&_objects.ContainsKey(_selectedObject))
            _objects[_selectedObject].ForEach(n=>n.SetActive(false));
        
        _selectedObject = TestObjects[objectNum];
        OnNumberEntered(_input.text);
    }

    void OnNumberEntered(string num)
    {
        int n = 0;

        if (!string.IsNullOrEmpty(num))
            n = int.Parse(num);
        else
        {
            n = 1;
            _input.text = "1";
        }

        if(_selectedObject != null &&_objects.ContainsKey(_selectedObject))
            _objects[_selectedObject].ForEach(n=>n.SetActive(false));

        CurrentColumn = CurrentRow = 0;
        
        for (int i = 0; i < n; i++)
        {
            var obj = GetObject();
            
            obj.transform.position = new Vector3(CurrentRow * ObjectSize, 0, CurrentColumn * ObjectSize);
            
            CurrentRow += 1;

            if (CurrentRow == Column)
            {
                CurrentColumn += 1;
                CurrentRow = 0;
            }
        }

        var selecterObjects = _objects[_selectedObject].Where(o => o.activeSelf).ToArray();

        Camera.SetTargets(selecterObjects);
    }

    GameObject GetObject()
    {
        if(!_objects.ContainsKey(_selectedObject))
            _objects.Add(_selectedObject, new List<GameObject>());
        
        for (int i = 0; i < _objects[_selectedObject].Count; i++)
        {
            if(_objects[_selectedObject][i].activeSelf) continue;

            _objects[_selectedObject][i].SetActive(true);
            
            return _objects[_selectedObject][i];
        }

        var newObj = Instantiate(_selectedObject, _selectedObject.transform.parent, true);
        newObj.SetActive(true);

        _objects[_selectedObject].Add(newObj);
        
        return newObj;
    }

    private void Update()
    {
        float timelapse = Time.smoothDeltaTime;
        timer = timer <= 0 ? refresh : timer -= timelapse;
 
        if(timer <= 0) avgFramerate = (int) (1f / timelapse);
        m_Text.text = string.Format(display,avgFramerate.ToString());
    }
}
