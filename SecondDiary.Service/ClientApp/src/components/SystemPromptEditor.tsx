import React, { useState, useEffect, ChangeEvent } from 'react';

interface SystemPromptEditorProps {
  token: string | null;
}

const SystemPromptEditor: React.FC<SystemPromptEditorProps> = ({ token }) => {
  const [systemPrompt, setSystemPrompt] = useState<string>('');
  const [lines, setLines] = useState<string[]>([]);
  const [editingIndex, setEditingIndex] = useState<number>(-1);
  const [editContent, setEditContent] = useState<string>('');
  const [message, setMessage] = useState<string>('');
  const [error, setError] = useState<string>('');

  useEffect(() => {
    fetchPrompt();
  }, [token]);

  useEffect(() => {
    fetchPrompt();
  }, []);

  const fetchPrompt = async (): Promise<void> => {
    if (!token) return;
    try {
      const response: Response = await fetch('api/SystemPrompt', {
        headers: { 'Authorization': `Bearer ${token}` }
      });
      if (response.ok) {
        const data = await response.json();
        setSystemPrompt(data.content);
        setLines(data.content.split('\n'));
      } else if (response.status === 404) {
        setSystemPrompt('');
        setLines(['']);
      } else setError('Failed to fetch system prompt');
    } catch (error) {
      if (error instanceof Error)
        setError('Error fetching system prompt: ' + error.message);
      else
        setError('Error fetching system prompt: ' + String(error));
    }
  };

  const savePrompt = async (): Promise<void> => {
    if (!token) return;
    try {
      const response: Response = await fetch('api/SystemPrompt', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({ content: systemPrompt })
      });
    } catch (error) {
      if (error instanceof Error)
        setError('Error saving system prompt: ' + error.message);
      else
        setError('Error saving system prompt: ' + String(error));
    }
  };

  const startEditLine = (index: number): void => {
    setEditingIndex(index);
    setEditContent(lines[index]);
  };

  const saveLine = async (): Promise<void> => {
    if (!token) return;
    try {
      const response: Response = await fetch(`api/SystemPrompt/lines/${editingIndex}`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify(editContent)
      });
      if (response.ok) {
        const newLines: string[] = [...lines];
        newLines[editingIndex] = editContent;
      } else {
        setError('Failed to update line');
      }
    } catch (error) {
      if (error instanceof Error) {
        setError('Error updating line: ' + error.message);
      } else {
        setError('Error updating line: ' + String(error));
      }
    }
    setMessage('Line updated successfully');
    setTimeout(() => setMessage(''), 3000);
  };

  const cancelEdit = (): void => setEditingIndex(-1);

  const addNewLine = (): void => {
    const newLines: string[] = [...lines, ''];
    setLines(newLines);
    setSystemPrompt(newLines.join('\n'));
    startEditLine(newLines.length - 1);
  };

  return (
    <div className="container">
      <h2 className="mt-4 mb-3">System Prompt Editor</h2>
      {message && <div className="alert alert-success">{message}</div>}
      {error && <div className="alert alert-danger">{error}</div>}
      <p>Edit your system prompt line by line or save the entire prompt at once.</p>
      <ul className="list-group mb-3">
        {lines.map((line, index) => (
          <li key={index} className="list-group-item">
            {editingIndex === index ? (
              <div className="row">
                <div className="col">
                  <textarea
                    className="form-control"
                    rows={3}
                    value={editContent}
                    onChange={(e: ChangeEvent<HTMLTextAreaElement>) => setEditContent(e.target.value)}
                  />
                </div>
                <div className="col-md-auto">
                  <button className="btn btn-success me-2" onClick={saveLine}>Save</button>
                  <button className="btn btn-secondary" onClick={cancelEdit}>Cancel</button>
                </div>
              </div>
            ) : (
              <div className="row">
                <div className="col">{line || <em>Empty line</em>}</div>
                <div className="col-md-auto">
                  <button className="btn btn-primary" onClick={() => startEditLine(index)}>Edit</button>
                </div>
              </div>
            )}
          </li>
        ))}
      </ul>
      <button className="btn btn-primary mb-3" onClick={addNewLine}>Add New Line</button>
      <form>
        <div className="mb-3">
          <label className="form-label">Complete System Prompt</label>
          <textarea
            className="form-control"
            rows={10}
            value={systemPrompt}
            onChange={(e: ChangeEvent<HTMLTextAreaElement>) => setSystemPrompt(e.target.value)}
          />
        </div>
        <button type="button" className="btn btn-primary" onClick={savePrompt}>Save Prompt</button>
      </form>
    </div>
  );
};

export { SystemPromptEditor };