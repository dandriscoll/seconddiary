import React, { useState, useEffect, ChangeEvent } from 'react';

interface SystemPromptEditorProps {
  token: string | null;
}

interface PromptLine {
  id: number;
  content: string;
}

const SystemPromptEditor: React.FC<SystemPromptEditorProps> = ({ token }) => {
  const [promptLines, setPromptLines] = useState<PromptLine[]>([]);
  const [newLineContent, setNewLineContent] = useState<string>('');
  const [message, setMessage] = useState<string>('');
  const [error, setError] = useState<string>('');

  useEffect(() => {
    fetchPromptLines();
  }, [token]);

  const fetchPromptLines = async (): Promise<void> => {
    if (!token) return;
    try {
      const response: Response = await fetch('api/SystemPrompt', {
        headers: { 'Authorization': `Bearer ${token}` }
      });
      
      if (response.ok) {
        const promptText: string = await response.text();
        // Split by newlines and convert to PromptLine objects
        const lines: string[] = promptText.split('\n');
        const promptLineObjects: PromptLine[] = lines.map((content, index) => ({
          id: index + 1, // Generate ID based on index
          content: content
        })).filter(line => line.content.trim() !== ''); // Filter out empty lines
        
        setPromptLines(promptLineObjects);
      } else if (response.status === 404) 
        setPromptLines([]);
      else 
        setError('Failed to fetch system prompt lines');
    } catch (error) {
      if (error instanceof Error)
        setError('Error fetching system prompt lines: ' + error.message);
      else
        setError('Error fetching system prompt lines: ' + String(error));
    }
  };

  const addNewLine = async (): Promise<void> => {
    if (!token || !newLineContent.trim()) return;
    
    try {
      // Get all current lines including the new one
      const response: Response = await fetch('api/systemPrompt', {
        method: 'POST',
        headers: {
          'Content-Type': 'text/plain',
          'Authorization': `Bearer ${token}`
        },
        body: newLineContent
      });
      
      if (response.ok) {
        // Add the new line to our state with a generated ID
        const newLineId: number = promptLines.length > 0 
          ? Math.max(...promptLines.map(line => line.id)) + 1 
          : 1;
          
        const newLine: PromptLine = {
          id: newLineId,
          content: newLineContent
        };
        
        setPromptLines([...promptLines, newLine]);
        setNewLineContent('');
        showSuccessMessage('Line added successfully');
      } else
        setError('Failed to add new line');
    } catch (error) {
      if (error instanceof Error)
        setError('Error adding new line: ' + error.message);
      else
        setError('Error adding new line: ' + String(error));
    }
  };

  const deleteLine = async (index: number): Promise<void> => {
    if (!token) return;
    
    try {
      // Filter out the line to be deleted and join the rest
      const updatedLines: PromptLine[] = [...promptLines];
      updatedLines.splice(index, 1);
      const updatedContent: string = updatedLines.map(line => line.content).join('\n');
      
      const response: Response = await fetch('api/SystemPrompt', {
        method: 'POST',
        headers: {
          'Content-Type': 'text/plain',
          'Authorization': `Bearer ${token}`
        },
        body: updatedContent
      });
      
      if (response.ok) {
        setPromptLines(updatedLines);
        showSuccessMessage('Line deleted successfully');
      } else
        setError('Failed to delete line');
    } catch (error) {
      if (error instanceof Error)
        setError('Error deleting line: ' + error.message);
      else
        setError('Error deleting line: ' + String(error));
    }
  };

  const showSuccessMessage = (msg: string): void => {
    setMessage(msg);
    setTimeout(() => setMessage(''), 3000);
  };

  return (
    <div className="container">
      <h2 className="mt-4 mb-3">System Prompt Editor</h2>
      {message && <div className="alert alert-success">{message}</div>}
      {error && <div className="alert alert-danger">{error}</div>}
      <p>Manage your system prompt lines by adding or removing lines.</p>
      
      <div className="card mb-4">
        <div className="card-header">Prompt Lines</div>
        <ul className="list-group list-group-flush">
          {promptLines.map((line, index) => (
            <li key={line.id} className="list-group-item">
              <div className="row">
                <div className="col">{line.content || <em>Empty line</em>}</div>
                <div className="col-md-auto">
                  <button className="btn btn-danger" onClick={() => deleteLine(index)}>Delete</button>
                </div>
              </div>
            </li>
          ))}
          {promptLines.length === 0 && (
            <li className="list-group-item text-center"><em>No prompt lines defined</em></li>
          )}
        </ul>
      </div>
      
      <div className="card mb-4">
        <div className="card-header">Add New Line</div>
        <div className="card-body">
          <div className="row">
            <div className="col">
              <input
                type="text"
                className="form-control"
                value={newLineContent}
                placeholder="Enter a new prompt line"
                onChange={(e: ChangeEvent<HTMLInputElement>) => setNewLineContent(e.target.value)}
              />
            </div>
            <div className="col-md-auto">
              <button 
                className="btn btn-primary" 
                onClick={addNewLine}
                disabled={!newLineContent.trim()}
              >
                Add Line
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export { SystemPromptEditor };