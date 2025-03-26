import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import './NavMenu.css';

const NavMenu: React.FC = () => {
    const [collapsed, setCollapsed] = useState<boolean>(true);

    const toggleNavbar = () => setCollapsed(!collapsed);

    return (
        <header>
            <nav className="navbar navbar-expand-sm navbar-toggleable-sm ng-white border-bottom box-shadow mb-3">
                <div className="container">
                    <Link className="navbar-brand" to="/">SecondDiary</Link>
                    <button onClick={toggleNavbar} className="navbar-toggler" type="button">
                        <span className="navbar-toggler-icon"></span>
                    </button>
                    <div className={`collapse navbar-collapse ${collapsed ? '' : 'show'}`}>
                        <ul className="navbar-nav flex-grow">
                            <li className="nav-item">
                                <Link className="nav-link text-dark" to="/">Home</Link>
                            </li>
                            <li className="nav-item">
                                <Link className="nav-link text-dark" to="/counter">Counter</Link>
                            </li>
                            <li className="nav-item">
                                <Link className="nav-link text-dark" to="/fetch-data">Fetch data</Link>
                            </li>
                            <li className="nav-item">
                                <Link className="nav-link text-dark" to="/system-prompt">System Prompt</Link>
                            </li>
                        </ul>
                    </div>
                </div>
            </nav>
        </header>
    );
}

export default NavMenu;