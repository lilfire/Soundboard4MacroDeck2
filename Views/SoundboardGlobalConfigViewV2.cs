using Soundboard4MacroDeck.Models;
using Soundboard4MacroDeck.Services;
using Soundboard4MacroDeck.ViewModels;
using SuchByte.MacroDeck.GUI.CustomControls;
using SuchByte.MacroDeck.Language;
using SuchByte.MacroDeck.Logging;
using SuchByte.MacroDeck.Plugins;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

namespace Soundboard4MacroDeck.Views;

internal enum SoundboardGlobalConfigViewV2Page { Output, Audio, Categories }

public partial class SoundboardGlobalConfigViewV2 : DialogForm
{
	private readonly SoundboardGlobalConfigViewModel _viewModel;
	public SoundboardGlobalConfigViewV2(MacroDeckPlugin plugin)
	{
		_viewModel = new(plugin);

		InitializeComponent();
		ApplyLocalization();
		SetCloseIconVisible(true);

		_viewModel.OnSetDeviceIndex += (_, _) => { comboBoxDevices.SelectedIndex = _viewModel.DevicesIndex; };
	}

	internal static SoundboardGlobalConfigViewV2 NewAtPage(SoundboardGlobalConfigViewV2Page page)
	{
		SoundboardGlobalConfigViewV2 view = new(PluginInstance.Current);
		switch (page)
		{
			case SoundboardGlobalConfigViewV2Page.Audio:
				view.navigation.SelectedTab = view.audioFilePage;
				break;
			case SoundboardGlobalConfigViewV2Page.Categories:
				view.navigation.SelectedTab = view.categoryPage;
				break;
			default:
				break;
		}
		return view;
	}

	private void ApplyLocalization()
	{
		outputPage.Text = LocalizationManager.Instance.GlobalConfigOutputDevice;
		audioFilePage.Text = LocalizationManager.Instance.GlobalConfigAudioFiles;
		categoryPage.Text = LocalizationManager.Instance.GlobalConfigAudioCategories;
		linkLabelResetDevice.Text = LocalizationManager.Instance.UseSystemDefaultDevice;
		labelDevices.Text = LocalizationManager.Instance.OutputDevicesGlobal;
		buttonOK.Text = LanguageManager.Strings.Ok;
		categoriesAdd.Image = SuchByte.MacroDeck.Properties.Resources.Create_Normal;
		audioFileAdd.Image = SuchByte.MacroDeck.Properties.Resources.Create_Normal;
	}

	private void SoundboardGlobalConfigView_Load(object sender, EventArgs e)
	{
		_viewModel.LoadDevices();
		comboBoxDevices.Items.AddRange(_viewModel.Devices.ToArray());
		_viewModel.LoadDeviceIndex();
		InitCategoriesPage();
		InitAudioFilesPage();
	}

	private void InitCategoriesPage()
	{
		categoriesAdd.Click += CategoriesAdd_Click;
		categoriesTable.Columns.Add(new DataGridViewTextBoxColumn
		{
			DataPropertyName = nameof(AudioCategory.Id),
			HeaderText = nameof(AudioCategory.Id),
			ReadOnly = true,
			AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
			Name = "Id"
		});

		categoriesTable.Columns.Add(new DataGridViewTextBoxColumn
		{
			DataPropertyName = nameof(AudioCategory.Name),
			HeaderText = nameof(AudioCategory.Name),
			AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
			Name = "Name"
		});


		categoriesTable.Columns.Add(new DataGridViewButtonColumn()
		{
			DataPropertyName = "Delete",
			HeaderText = "Delete",
			Name = "Delete",
			AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
		});

		//categoriesTable.DataSource = _viewModel.AudioCategories;

		BuildCategoryTable();




		categoriesTable.CellEndEdit += CategoriesTable_CellEndEdit;
	}

	private void BuildCategoryTable()
	{
		categoriesTable.Rows.Clear();
		foreach (var audioCategory in _viewModel.AudioCategories)
		{
			var dataGridViewRow = new DataGridViewRow();
			dataGridViewRow.Cells.Add(new DataGridViewTextBoxCell() { Value = audioCategory.Id });
			dataGridViewRow.Cells.Add(new DataGridViewTextBoxCell() { Value = audioCategory.Name });
			dataGridViewRow.Cells.Add(new DataGridViewButtonCell() { Value = "Delete" });

			categoriesTable.Rows.Add(dataGridViewRow);
		}
	}

	private BindingList<AudioCategory> categoryComboBoxList;

	//private BindingList<AudioFileItem> audioFilesList;
	private void InitAudioFilesPage()
	{
		audioFileAdd.Click += AudioFileAdd_Click;
		audioFilesTable.Columns.Add(new DataGridViewTextBoxColumn
		{
			DataPropertyName = nameof(AudioFileItem.Id),
			HeaderText = nameof(AudioFileItem.Id),
			ReadOnly = true,
			AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader,
			Name = "Id"
		});

		audioFilesTable.Columns.Add(new DataGridViewTextBoxColumn
		{
			DataPropertyName = nameof(AudioFileItem.Name),
			HeaderText = nameof(AudioFileItem.Name),
			Name = "Name"
		});

		categoryComboBoxList = new(_viewModel.AudioCategories);
		DataGridViewComboBoxColumn categoryBox = new()
		{
			DataPropertyName = nameof(AudioFileItem.CategoryId),
			HeaderText = "Category",
			DisplayMember = nameof(AudioCategory.Name),  // Display the 'Name' property of the AudioCategory
			ValueMember = nameof(AudioCategory.Id),  // Use the 'Id' property of the AudioCategory as the actual value
			DataSource = categoryComboBoxList,
			Name = "Category"
		};
		audioFilesTable.Columns.Add(categoryBox);

		audioFilesTable.Columns.Add(new DataGridViewButtonColumn()
		{
			DataPropertyName = "Delete",
			HeaderText = "Delete",
			Name = "Delete",
			AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
		});


		//audioFilesList = new(_viewModel.AudioFiles);
		//audioFilesTable.DataSource = audioFilesList;
		BuildAudioFilesTable();


		audioFilesTable.CellEndEdit += AudioFilesTable_CellEndEdit;
	}

	private void BuildAudioFilesTable()
	{
		audioFilesTable.Rows.Clear();
		foreach (var audioFileItem in _viewModel.AudioFiles)
		{
			var dataGridViewRow = new DataGridViewRow();
			dataGridViewRow.Cells.Add(new DataGridViewTextBoxCell() { Value = audioFileItem.Id });
			dataGridViewRow.Cells.Add(new DataGridViewTextBoxCell() { Value = audioFileItem.Name });
			var dataGridViewComboBoxCell = new DataGridViewComboBoxCell()
			{
				DisplayMember = nameof(AudioCategory.Name),  // Display the 'Name' property of the AudioCategory
				ValueMember = nameof(AudioCategory.Id),  // Use the 'Id' property of the AudioCategory as the actual value
				DataSource = categoryComboBoxList,
				Value = audioFileItem.CategoryId
			};
			dataGridViewRow.Cells.Add(dataGridViewComboBoxCell);
			dataGridViewRow.Cells.Add(new DataGridViewButtonCell() { Value = "Delete" });

			audioFilesTable.Rows.Add(dataGridViewRow);
		}
	}


	private void AudioFileAdd_Click(object sender, EventArgs e)
	{
		using var audioAddDialog = new SoundboardGlobalAudioAddView(_viewModel);
		if (audioAddDialog.ShowDialog(this) == DialogResult.OK)
		{
			//TODO audioFilesList.Add(_viewModel.LastAudioFile.ToAudioFileItem());
			_viewModel.LastAudioFile = null;
			BuildAudioFilesTable();
		}
	}

	private void Navigation_Selecting(object sender, TabControlCancelEventArgs e)
	{
		if (e.TabPage.Name == audioFilePage.Name)
		{
			// Refresh audioCategories
			categoryComboBoxList.Clear();
			foreach (var cat in _viewModel.AudioCategories)
			{
				categoryComboBoxList.Add(cat);
			}
		}
	}

	private void CategoriesAdd_Click(object sender, EventArgs e)
	{
		PluginInstance.DbContext.InsertAudioCategory(new());
		BuildCategoryTable();
	}
	private void CategoriesTable_CellEndEdit(object sender, DataGridViewCellEventArgs e)
	{
		try
		{
			var editedRow = categoriesTable.Rows[e.RowIndex];
			//AudioCategory editedCategory = (AudioCategory)editedRow.DataBoundItem;
			AudioCategory editedCategory = _viewModel.AudioCategories[e.RowIndex];

			editedCategory.Name = editedRow.Cells["Name"].Value.ToString();

			_viewModel.UpdateCategory(editedCategory);

		}
		catch (Exception ex)
		{
			MacroDeckLogger.Trace(PluginInstance.Current, typeof(SoundboardGlobalConfigViewV2), ex.Message);
		}
	}

	private void AudioFilesTable_CellEndEdit(object sender, DataGridViewCellEventArgs e)
	{
		try
		{
			var editedRow = audioFilesTable.Rows[e.RowIndex];
			//AudioFileItem editedItem = (AudioFileItem)editedRow.DataBoundItem;

			AudioFileItem audioFile = _viewModel.AudioFiles[e.RowIndex];
			audioFile.Name = editedRow.Cells["Name"].Value.ToString();
			audioFile.CategoryId = (int)editedRow.Cells["Category"].Value;

			_viewModel.UpdateAudioFile(audioFile);
		}
		catch (Exception ex)
		{
			MacroDeckLogger.Trace(PluginInstance.Current, typeof(SoundboardGlobalConfigViewV2), ex.Message);
		}
	}

	private void ComboBoxDevices_SelectedIndexChanged(object sender, EventArgs e)
	{
		_viewModel.SetDevice(comboBoxDevices.SelectedIndex);
	}

	private void ButtonOK_Click(object sender, EventArgs e)
	{
		audioFilesTable.EndEdit();
		categoriesTable.EndEdit();
		_viewModel.SaveConfig();
	}

	private void LinkLabelResetDevice_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
	{
		_viewModel.ResetDevice();
	}

	private void categoriesTable_CellClick(object sender, DataGridViewCellEventArgs e)
	{
		if (e.RowIndex < 0)
			return;

		var dataGridViewRow = categoriesTable.Rows[e.RowIndex];
		var cell = dataGridViewRow.Cells[e.ColumnIndex];

		if (cell is DataGridViewButtonCell && e.RowIndex < _viewModel.AudioCategories.Count)
		{
			AudioCategory category = _viewModel.AudioCategories[e.RowIndex];

			var audioFiles = _viewModel.AudioFiles.Where(a => a.CategoryId == category.Id).ToList();
			if (audioFiles.Any())
			{
				throw new Exception($"Cannot delete category {category.Id} {category.Name}. Used on {string.Join(",", audioFiles.Select(a => a.Name))} ");
			}

			_viewModel.DeleteCategory(category);
			BuildCategoryTable();
		}
	}

	private void audioFilesTable_CellClick(object sender, DataGridViewCellEventArgs e)
	{
		if (e.RowIndex < 0)
			return;

		var dataGridViewRow = audioFilesTable.Rows[e.RowIndex];
		var cell = dataGridViewRow.Cells[e.ColumnIndex];

		if (cell is DataGridViewButtonCell && e.RowIndex < _viewModel.AudioFiles.Count)
		{
			AudioFileItem audioFile = _viewModel.AudioFiles[e.RowIndex];
			
			_viewModel.DeleteAudioFile(audioFile);
			BuildAudioFilesTable();
		}
	}
}
