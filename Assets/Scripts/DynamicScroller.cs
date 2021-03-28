using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DynamicScroller : MonoBehaviour
{
    public Unit proto;

    public Text scrollIndex;
    public Text changeUnit;
    public Text addUnit;

    /// <summary>
    /// Current upper visible index
    /// </summary>
    private int currentIndex;

    /// <summary>
    /// Size of unit on the scene
    /// </summary>
    private float unitSize;
    /// <summary>
    /// Scroll step for one mouse scroll
    /// </summary>
    private readonly float scrollStep = 20f;
    private bool blockedActions = false;

    //Можно было бы создать отдельный класс для получения данных откуда-то там, но для простоты пусть будет так
    /// <summary>
    /// Actual list of content
    /// </summary>
    private List<string> contentBank = new List<string>();
    /// <summary>
    /// Unit block on the scene
    /// </summary>
    private List<Unit> unitBlock = new List<Unit>();

    /// <summary>
    /// Value of Y when upper unit is fully visible
    /// </summary>
    private float startY => unitSize - GetComponent<VerticalLayoutGroup>().spacing;
    /// <summary>
    /// Value of Y when upper unit is about to hide
    /// </summary>
    private float endY => startY + unitSize;
    /// <summary>
    /// Size of viewport on the scene
    /// </summary>
    private float viewportSize => transform.parent.GetComponent<RectTransform>().rect.height;
    /// <summary>
    /// Spacing between units in the viewport
    /// </summary>
    private float unitSpacing => GetComponent<VerticalLayoutGroup>().spacing;
    /// <summary>
    /// Actual amount of units on the scene
    /// </summary>
    private int visibleBlockSize => Mathf.FloorToInt(viewportSize / unitSize) + 1;
    /// <summary>
    /// Maximum index of upper unit when the last unit is exactly in the bottom
    /// </summary>
    private int maximumScrollReachIndex => viewportSize - unitSize * visibleBlockSize - unitSpacing < 0.1f ?
        contentBank.Count - visibleBlockSize + 1 : contentBank.Count - visibleBlockSize;
    /// <summary>
    /// Value of Y when last unit is exactly in the bottom
    /// </summary>
    private float endPosY => (unitSize - (viewportSize - (visibleBlockSize - 1) * unitSize - unitSpacing)) % unitSize + startY;

    private void Start()
    {
        unitSize = proto.GetComponent<RectTransform>().rect.height + unitSpacing;

        //filling content
        for (int i = 1; i < 8; ++i)
        {
            contentBank.Add(i.ToString());
        }

        CreateUnits();

        GetComponent<RectTransform>().anchoredPosition = new Vector2(0, startY);
        currentIndex = 0;

        UpdateContent();
    }

    private void CreateUnits()
    {
        foreach (var unit in unitBlock)
            Destroy(unit.gameObject);
        unitBlock.Clear();
        var amount = contentBank.Count < visibleBlockSize ? contentBank.Count + 1 : visibleBlockSize + 2;
        for (int i = 0; i < amount; ++i)
        {
            var unit = Instantiate(proto);
            unit.transform.SetParent(transform);
            unitBlock.Add(unit);
        }
    }

    private void Update()
    {
        if (blockedActions)
            return;
        var mouseDelta = Input.GetAxis("Scroll");
        if (Mathf.Abs(mouseDelta) > 0.01f)
        {
            FixedScroll(mouseDelta);
        }
    }

    private void UpdateContent()
    {
        for (int i = currentIndex; i < currentIndex + unitBlock.Count; ++i)
        {
            if (i == 0 || i > contentBank.Count)
                continue;
            var j = i - currentIndex;
            unitBlock[j].UpdateContent(i, contentBank[i - 1]);
        }
    }

    //как-то все лаконично уместилось в один скрипт, так что кнопки я решил тоже оставить здесь
    #region -----Button handlers-----

    public void ScrollToIndex()
    {
        if (!int.TryParse(scrollIndex.text, out var index))
            return;
        if (index <= 0 || index > contentBank.Count || blockedActions)
        {
            return;
        }
        StartCoroutine(ScrollingTo(index - 1));
        scrollIndex.text = "";
    }

    public void AddUnit()
    {
        contentBank.Add(addUnit.text);
        addUnit.text = "";
        CreateUnits();
        UpdateContent();
        DeselectAll();
    }

    public void ChangeUnit()
    {
        var _changeUnit = unitBlock.Find(x => x.Selected);
        if (_changeUnit == null)
            return;
        _changeUnit.content.text = changeUnit.text;
        contentBank[_changeUnit.tabindex - 1] = changeUnit.text;
        changeUnit.text = "";
    }
    public void DeleteUnit()
    {
        var _changeUnit = unitBlock.Find(x => x.Selected);
        if (_changeUnit == null)
            return;
        contentBank.RemoveAt(_changeUnit.tabindex - 1);
        if (currentIndex > maximumScrollReachIndex)
        {
            currentIndex = Mathf.Max(0, maximumScrollReachIndex);
        }
        CreateUnits();
        UpdateContent();
        DeselectAll();
    }

    #endregion

    private void DeselectAll()
    {
        foreach (Unit _unit in unitBlock)
            _unit.Select(false);
    }


    private void FixedScroll(float delta, int step = 1, int reachIndex = -1)
    {
        DeselectAll();

        if (contentBank.Count < visibleBlockSize)
            return;

        //current position
        var rectPos = GetComponent<RectTransform>().anchoredPosition;
        //next position after scroll step
        var nextPosY = rectPos.y - Mathf.Sign(delta) * scrollStep;

        //scroll up
        if (nextPosY < startY)
        {
            //scrolled to the beginning
            if (currentIndex == 0)
            {
                nextPosY = startY;
            }
            //scroll up
            else
            {
                nextPosY += unitSize;
                currentIndex = step == 1 ? currentIndex - 1 : Mathf.Max(reachIndex, currentIndex - step);
                UpdateContent();
            }
        }

        //scrolled to the end
        else if (currentIndex >= maximumScrollReachIndex && nextPosY > endPosY)
        {
            currentIndex = maximumScrollReachIndex;
            nextPosY = endPosY;
        }

        //scroll down
        else if (nextPosY >= endY)
        {
            nextPosY -= unitSize;
            currentIndex = step == 1 ? currentIndex + 1 : Mathf.Min(reachIndex, Mathf.Min(currentIndex + step, maximumScrollReachIndex));
            if (currentIndex >= maximumScrollReachIndex && nextPosY > endPosY)
            {
                currentIndex = maximumScrollReachIndex;
                nextPosY = endPosY;
            }
            UpdateContent();
        }

        //when scroller tries to go lower than bottom limit
        if (reachIndex == currentIndex ||
            reachIndex > maximumScrollReachIndex && currentIndex == maximumScrollReachIndex)
        {
            nextPosY = startY;
        }

        GetComponent<RectTransform>().anchoredPosition = new Vector2(0, nextPosY);
    }


    IEnumerator ScrollingTo(int index)
    {
        if (index - currentIndex == 0 || contentBank.Count < visibleBlockSize)
        {
            foreach (Unit _unit in unitBlock)
                _unit.Select(_unit.tabindex == index + 1);
            yield break;
        }
        blockedActions = true;
        var startIndex = currentIndex;
        var scrollTime = Mathf.Min(Mathf.Abs((index - startIndex) / 40f), 2.0f);
        var _time = 0f;
        var sign = -Mathf.Sign(index - currentIndex);
        while (currentIndex != Mathf.Min(index, maximumScrollReachIndex))
        {
            var step = Mathf.Abs(currentIndex - Mathf.RoundToInt(startIndex - sign * _time * Mathf.Abs(index - startIndex)));
            FixedScroll(sign, step, index);
            _time += Time.deltaTime / scrollTime;
            yield return null;
        }
        foreach (Unit _unit in unitBlock)
            _unit.Select(_unit.tabindex == index + 1);
        blockedActions = false;
    }
}
