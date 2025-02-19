﻿using SoulsFormats;
using System;
using System.Linq;
using System.Windows.Forms;

namespace TAEDX.TaeEditor
{
    public partial class TaeInspectorWinFormsControl : UserControl
    {
        
        public event EventHandler<EventArgs> TaeEventValueChanged;

        protected virtual void OnTaeEventValueChanged(EventArgs e)
        {
            TaeEventValueChanged?.Invoke(this, e);
        }

        private TaeEditAnimEventBox _selectedEventBox;
        public TaeEditAnimEventBox SelectedEventBox
        {
            get => _selectedEventBox;
            set
            {
                DumpDataGridValuesToEvent();

                dataGridView1.CellValueChanged -= DataGridView1_CellValueChanged;
                _selectedEventBox = value;
                ReconstructDataGrid();


            }
        }

        private void ReconstructDataGrid()
        {
            dataGridView1.Rows.Clear();

            if (SelectedEventBox != null)
            {
                var ev = SelectedEventBox.MyEvent;

                if (ev.Template != null)
                {
                    foreach (var p in ev.Parameters.Template.Where(kvp => kvp.Value.ValueToAssert == null))
                    {
                        

                        if (p.Value.EnumEntries != null)
                        {
                            var cell = new DataGridViewComboBoxCell();
                            cell.DataSource = p.Value.EnumEntries.Keys.ToList();

                            cell.Value = p.Value.ValueToString(ev.Parameters[p.Key]);

                            var row = new DataGridViewRow();

                            row.Cells.Add(new DataGridViewTextBoxCell() { Value = p.Value.Type });
                            row.Cells.Add(new DataGridViewTextBoxCell() { Value = p.Value.Name });
                            row.Cells.Add(cell);

                            dataGridView1.Rows.Add(row);
                        }
                        else if (p.Value.Type == TAE.Template.ParamType.b)
                        {
                            var cell = new DataGridViewCheckBoxCell();
                            cell.Value = (bool)ev.Parameters[p.Key];

                            var row = new DataGridViewRow();

                            row.Cells.Add(new DataGridViewTextBoxCell() { Value = p.Value.Type });
                            row.Cells.Add(new DataGridViewTextBoxCell() { Value = p.Value.Name });
                            row.Cells.Add(cell);

                            dataGridView1.Rows.Add(row);
                        }
                        else
                        {
                            //var cell = new DataGridViewTextBoxCell();

                            //cell.Value = p.Value.ValueToString(ev.Parameters[p.Key]);

                            //var row = new DataGridViewRow();

                            //row.Cells.Add(new DataGridViewTextBoxCell() { Value = p.Value.Type });
                            //row.Cells.Add(new DataGridViewTextBoxCell() { Value = p.Value.Name });
                            //row.Cells.Add(cell);

                            //dataGridView1.Rows.Add(row);

                            dataGridView1.Rows.Add(p.Value.Type, p.Value.Name, p.Value.ValueToString(ev.Parameters[p.Key]));
                        }

                        //dataGridView1.Rows[dataGridView1.RowCount - 1].Cells[2].ValueType = p.Value.GetValueObjectType();
                    }

                }
                else
                {
                    var bytes = ev.GetParameterBytes(SelectedEventBox.OwnerPane.MainScreen.SelectedTae.BigEndian);

                    for (int i = 0; i < bytes.Length; i++)
                    {
                        dataGridView1.Rows.Add(TAE.Template.ParamType.x8, $"Byte[{i}]", bytes[i].ToString("X2"));
                        //dataGridView1.Rows[dataGridView1.RowCount - 1].Cells[2].ValueType = typeof(byte);
                    }
                }

                dataGridView1.CellValueChanged += DataGridView1_CellValueChanged;
                dataGridView1.CellValidating += DataGridView1_CellValidating;
            }
        }

        private void DataGridView1_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            SaveDataGridRowValueToEvent(e.RowIndex);
        }

        private void DataGridView1_CellValuePushed(object sender, DataGridViewCellValueEventArgs e)
        {
            SaveDataGridRowValueToEvent(e.RowIndex);
        }

        private void DataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            SaveDataGridRowValueToEvent(e.RowIndex);
        }

        private bool SaveDataGridRowValueToEvent(int row)
        {
            if (SelectedEventBox == null)
                return true;

            var r = dataGridView1.Rows[row];
            var ev = SelectedEventBox.MyEvent;

            if (ev.Template != null)
            {
                var name = (string)r.Cells[1].Value;
                var value = (dataGridView1.IsCurrentCellDirty ? r.Cells[2].EditedFormattedValue.ToString() : r.Cells[2].FormattedValue.ToString());

                try
                {
                    ev.Parameters[name] = ev.Template[name].StringToValue(value);
                    r.Cells[2].ErrorText = string.Empty;
                }
                catch (InvalidCastException)
                {
                    r.Cells[2].ErrorText = "Invalid value type entered.";
                    dataGridView1.UpdateCellErrorText(2, row);
                    return false;
                }
                catch (OverflowException)
                {
                    r.Cells[2].ErrorText = "Value entered is too large for this value type to hold.";
                    dataGridView1.UpdateCellErrorText(2, row);
                    return false;
                }
                catch (FormatException)
                {
                    r.Cells[2].ErrorText = "Value is in the wrong format.";
                    dataGridView1.UpdateCellErrorText(2, row);
                    return false;
                }
                catch (Exception)
                {
                    r.Cells[2].ErrorText = "Failed to store value. Make sure it is in a valid format.";
                    dataGridView1.UpdateCellErrorText(2, row);
                    return false;
                }
            }
            else
            {
                try
                {
                    byte[] bytes = SelectedEventBox.MyEvent.GetParameterBytes(SelectedEventBox.OwnerPane.MainScreen.SelectedTae.BigEndian);
                    bytes[row] = byte.Parse(dataGridView1.IsCurrentCellDirty ? r.Cells[2].EditedFormattedValue.ToString() : r.Cells[2].FormattedValue.ToString(), System.Globalization.NumberStyles.HexNumber);
                    SelectedEventBox.MyEvent.SetParameterBytes(SelectedEventBox.OwnerPane.MainScreen.SelectedTae.BigEndian, bytes);
                    r.Cells[2].ErrorText = string.Empty;
                }
                catch (InvalidCastException)
                {
                    r.Cells[2].ErrorText = "Invalid value type entered.";
                    dataGridView1.UpdateCellErrorText(2, row);
                    return false;
                }
                catch (OverflowException)
                {
                    r.Cells[2].ErrorText = "Value entered is too large for this value type to hold.";
                    dataGridView1.UpdateCellErrorText(2, row);
                    return false;
                }
                catch (FormatException)
                {
                    r.Cells[2].ErrorText = "Value is in the wrong format.";
                    dataGridView1.UpdateCellErrorText(2, row);
                    return false;
                }
                catch (Exception)
                {
                    r.Cells[2].ErrorText = "Failed to store value. Make sure it is in a valid format.";
                    dataGridView1.UpdateCellErrorText(2, row);
                    return false;
                }
            }

            SelectedEventBox.UpdateEventText();

            OnTaeEventValueChanged(EventArgs.Empty);

            return true;
        }

        public bool DumpDataGridValuesToEvent()
        {
            bool failed = false;

            if (SelectedEventBox != null)
            {
                for (int i = 0; i < dataGridView1.RowCount; i++)
                {
                    if (!SaveDataGridRowValueToEvent(i))
                        failed = true;
                }
            }

            return !failed;
        }

        public TaeInspectorWinFormsControl()
        {
            InitializeComponent();

            dataGridView1.SelectionMode = DataGridViewSelectionMode.CellSelect;

            dataGridView1.AllowUserToResizeRows = false;

            dataGridView1.EditingControlShowing += DataGridView1_EditingControlShowing;

            dataGridView1.AllowUserToOrderColumns = false;
        }

        IDataGridViewEditingControl _iDataGridViewEditingControl;
        private void DataGridView1_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (_iDataGridViewEditingControl is DataGridViewTextBoxEditingControl)
            {
                DataGridViewTextBoxEditingControl iDataGridViewEditingControl = _iDataGridViewEditingControl as DataGridViewTextBoxEditingControl;
                iDataGridViewEditingControl.KeyPress -= Control_KeyPress;
            }
            if (e.Control is DataGridViewTextBoxEditingControl)
            {
                DataGridViewTextBoxEditingControl iDataGridViewEditingControl = e.Control as DataGridViewTextBoxEditingControl;
                iDataGridViewEditingControl.KeyPress += Control_KeyPress;
                _iDataGridViewEditingControl = iDataGridViewEditingControl;
            }
        }

        private void Control_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (dataGridView1.IsCurrentCellInEditMode)
            {
                if (dataGridView1.SelectedCells.Count != 1)
                    return;

                var selectedCell = dataGridView1.SelectedCells[0];

                

                string paramName = selectedCell.OwningRow.Cells[1].FormattedValue.ToString();
                TAE.Template.ParamType p = TAE.Template.ParamType.x8;

                switch (e.KeyChar)
                {
                    case (char)Keys.Back:
                    case (char)Keys.Enter:
                        break;
                    case ' ':
                        if (SelectedEventBox.MyEvent.Template == null)
                        {
                            e.Handled = true;
                            BEEP();
                            break;
                        }
                        else
                        {
                            p = SelectedEventBox.MyEvent.Parameters.Template[paramName].Type;
                            if (p != TAE.Template.ParamType.aob)
                            {
                                e.Handled = true;
                                BEEP();
                            }
                        }
                        break;
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        break;
                    case '-':
                        if (SelectedEventBox.MyEvent.Template == null)
                        {
                            e.Handled = true;
                            BEEP();
                            break;
                        }
                        else
                        {
                            p = SelectedEventBox.MyEvent.Parameters.Template[paramName].Type;
                            switch (p)
                            {
                                case TAE.Template.ParamType.s8:
                                case TAE.Template.ParamType.s16:
                                case TAE.Template.ParamType.s32:
                                case TAE.Template.ParamType.s64:
                                case TAE.Template.ParamType.f32:
                                case TAE.Template.ParamType.f64:
                                    break;
                                default:
                                    e.Handled = true;
                                    BEEP();
                                    break;
                            }
                        }
                        break;
                    case 'a':
                    case 'b':
                    case 'c':
                    case 'd':
                    case 'e':
                    case 'f':
                    case 'A':
                    case 'B':
                    case 'C':
                    case 'D':
                    case 'E':
                    case 'F':
                        if (SelectedEventBox.MyEvent.Template == null)
                        {
                            switch (e.KeyChar)
                            {
                                case 'a': e.KeyChar = 'A'; break;
                                case 'b': e.KeyChar = 'B'; break;
                                case 'c': e.KeyChar = 'C'; break;
                                case 'd': e.KeyChar = 'D'; break;
                                case 'e': e.KeyChar = 'E'; break;
                                case 'f': e.KeyChar = 'F'; break;
                                default: break;
                            }

                            break;
                        }

                        p = SelectedEventBox.MyEvent.Parameters.Template[paramName].Type;
                        switch (p)
                        {
                            case TAE.Template.ParamType.x8:
                            case TAE.Template.ParamType.x16:
                            case TAE.Template.ParamType.x32:
                            case TAE.Template.ParamType.x64:
                            case TAE.Template.ParamType.aob:
                                switch (e.KeyChar)
                                {
                                    case 'a': e.KeyChar = 'A'; break;
                                    case 'b': e.KeyChar = 'B'; break;
                                    case 'c': e.KeyChar = 'C'; break;
                                    case 'd': e.KeyChar = 'D'; break;
                                    case 'e': e.KeyChar = 'E'; break;
                                    case 'f': e.KeyChar = 'F'; break;
                                    default: break;
                                }
                                break;
                            default:
                                e.Handled = true;
                                BEEP();
                                break;
                        }
                        break;
                    case ',':
                    case '.':
                        if (SelectedEventBox.MyEvent.Template == null)
                        {
                            e.Handled = true;
                            BEEP();
                            break;
                        }

                        var isValidFloatingPoint = (System.Threading.Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator.Contains(e.KeyChar));
                        p = SelectedEventBox.MyEvent.Parameters.Template[paramName].Type;
                        if ((p != TAE.Template.ParamType.f32 && p != TAE.Template.ParamType.f64) || !isValidFloatingPoint)
                        {
                            e.Handled = true;
                            BEEP();
                        }
                        break;
                    default:
                        e.Handled = true;
                        BEEP();
                        break;
                }
            }
        }

        private void BEEP()
        {
            System.Media.SystemSounds.Beep.Play();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}
